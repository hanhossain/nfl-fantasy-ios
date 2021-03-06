﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Counsel.Core.Nfl;

namespace Counsel.Core.Database
{
	public interface IFantasyDatabase
	{
		// TODO: update this with season + week
		Task<bool> PlayersExistAsync();

		Task UpdatePlayersAsync(int season, int week, IEnumerable<NflAdvancedStats> advancedStats, IDictionary<string, NflPlayerStats> weekStats);

		Task<IEnumerable<Player>> GetPlayersAsync();

		Task<IEnumerable<Statistics>> GetStatisticsAsync(string playerId);

		Task<IEnumerable<Player>> SearchPlayersAsync(string query);

		Task<IEnumerable<(Player Player, double Points)>> GetOpposingPlayersAsync(int season, int week, string playerId);
	}
}
