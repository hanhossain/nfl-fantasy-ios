﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Counsel.Core.Espn;
using Counsel.Core.Models;
using Counsel.Core.Nfl;

namespace Counsel.Core
{
	public class FantasyService : IFantasyService
	{
		private readonly AsyncLock _asyncLock = new AsyncLock();
		private readonly Database.IFantasyDatabase _fantasyDatabase;
		private readonly IEspnClient _espnClient;
		private readonly INflClient _nflClient;

		private int? _season;
		private int? _week;

		public FantasyService(Database.IFantasyDatabase fantasyDatabase, IEspnClient espnClient, INflClient nflClient)
		{
			_fantasyDatabase = fantasyDatabase;
			_espnClient = espnClient;
			_nflClient = nflClient;
		}

		public async Task UpdateAsync()
		{
			(int season, int week) = await GetCurrentWeekAsync();

			if (!await _fantasyDatabase.PlayersExistAsync())
			{
				var advancedSeasonTasks = Enumerable.Range(1, week)
					.Select(x => _nflClient.GetAdvancedStatsAsync(season, x))
					.ToList();

				var advancedSeasonStats = await Task.WhenAll(advancedSeasonTasks);

				var statsTasks = Enumerable.Range(1, week)
					.Select(x => _nflClient.GetStatsAsync(season, x))
					.ToList();

				var seasonStats = await Task.WhenAll(statsTasks);

				foreach (var (advancedWeekStats, weekStats) in advancedSeasonStats.Zip(seasonStats, (x, y) => (x, y)))
				{
					await _fantasyDatabase.UpdatePlayersAsync(
						weekStats.Season,
						weekStats.Week,
						advancedWeekStats.Values.SelectMany(x => x),
						weekStats.Players.ToDictionary(x => x.Id, x => x));
				}
			}
		}

		public async Task<Dictionary<string, Player>> GetPlayersAsync()
		{
			var players = await _fantasyDatabase.GetPlayersAsync();

			return players.Select(x => new Player()
			{
				Id = x.Id,
				FirstName = x.FirstName,
				LastName = x.LastName,
				Position = x.Position,
				Team = x.Team
			}).ToDictionary(x => x.Id, x => x);
		}

		public async Task<(int Season, int Week)> GetCurrentWeekAsync()
		{
			if (_season == null || _week == null)
			{
				using var lockToken = await _asyncLock.LockAsync();

				if(_season == null || _week == null)
				{
					var seasons = await _espnClient.GetSeasonsAsync();
					var recentSeason = seasons.First();
					_season = recentSeason.Id;
					_week = recentSeason.CurrentScoringPeriod.Id;
				}
			}

			return (_season.Value, _week.Value);
		}

		public async Task<PlayerStats> GetStatsAsync(string playerId)
		{
			var statistics = await _fantasyDatabase.GetStatisticsAsync(playerId);
			var result = new PlayerStats()
			{
				Weeks = statistics.Select(x => x.Week).ToList(),
				Season = statistics.First().Season,
				Points = statistics.Select(x => (x.Points, x.ProjectedPoints)).ToList(),
				PlayerId = playerId
			};

			var (_, week) = await GetCurrentWeekAsync();

			// calculated stats requires at least one week to have passed
			if (week > 1)
			{
				var points = result.Points.Select(x => x.Points).ToList();

				result.Mean = points.Average();
				result.Median = GetMedian(points);
				result.Max = points.Max();
				result.Min = points.Min();
				result.Range = result.Max - result.Min;
				result.PopStdDev = Math.Sqrt(points.Sum(x => Math.Pow(x - result.Mean, 2)) / points.Count);
			}

			return result;
		}

		public async Task<Dictionary<string, Player>> SearchPlayersAsync(string playerName)
		{
			var players = await _fantasyDatabase.SearchPlayersAsync(playerName);

			return players
				.Select(x => new Player()
				{
					FirstName = x.FirstName,
					LastName = x.LastName,
					Id = x.Id,
					Position = x.Position,
					Team = x.Team
				})
				.ToDictionary(x => x.Id, x => x);
		}

		public async Task<Dictionary<string, List<(Player Player, double Points)>>> GetOpponentsAsync(string playerId, int season, int week)
		{
			var opposingPlayers = await _fantasyDatabase.GetOpposingPlayersAsync(season, week, playerId);

			return opposingPlayers
				.Select(x => (Player: new Player()
				{
					FirstName = x.Player.FirstName,
					LastName = x.Player.LastName,
					Id = x.Player.Id,
					Position = x.Player.Position,
					Team = x.Player.Team
				}, x.Points))
				.GroupBy(x => x.Player.Position)
				.ToDictionary(
					x => x.Key,
					x => x.OrderBy(y => y.Player.LastName)
						.ThenBy(y => y.Player.FirstName)
						.ToList());
		}

		private static double GetMedian(List<double> values)
		{
			if (!values.Any())
			{
				throw new InvalidOperationException($"{nameof(values)} contains no elements");
			}

			var sorted = values.OrderBy(x => x).ToList();
			int middleIndex = sorted.Count / 2;
			double middleValue = sorted[middleIndex];

			if (sorted.Count % 2 == 0)
			{
				double otherMiddle = sorted[middleIndex + 1];

				middleValue = (middleValue + otherMiddle) / 2.0;
			}

			return middleValue;
		}
	}
}
