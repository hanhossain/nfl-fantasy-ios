//
//  NFLService.swift
//  nfl-fantasy-ios
//
//  Created by Han Hossain on 10/8/18.
//  Copyright © 2018 Han Hossain. All rights reserved.
//

import Foundation

class NFLService {
	
	private let decoder = JSONDecoder()
	private let baseAddress = "https://api.fantasy.nfl.com/v1/"
	
	func getStatistics(season: Int, week: Int, completion: @escaping (NFLStatistics?, String?) -> ()) {
		let query = "players/stats?statType=weekStats&season=\(season)&week=\(week)"
		
		makeRequest(query: query) { (stats, error) in
			completion(stats, error)
		}
	}
	
	func getLastCompletedWeek(completion: @escaping ((Int, Int)?, String?) -> ()) {
		getCurrentWeek { (weekInfo, error) in
			guard let (currentWeek, currentSeason) = weekInfo else {
				completion(nil, error)
				return
			}
			
			self.isWeekComplete(currentWeek, season: currentSeason) { (isComplete, error) in
				guard let isComplete = isComplete else {
					completion(nil, error)
					return
				}
				
				let completedWeek = isComplete ? currentWeek : currentWeek - 1
				
				guard currentWeek > 0 else {
					completion(nil, "no completed weeks in season yet")
					return
				}
				
				completion((completedWeek, currentSeason), nil)
			}
		}
	}
	
	private func getCurrentWeek(completion: @escaping ((Int, Int)?, String?) -> ()) {
		guard let url = URL(string: baseAddress + "players/stats") else {
			completion(nil, "url was invalid")
			return
		}
		
		URLSession.shared.dataTask(with: url) { (data, response, error) in
			guard let data = data, error == nil else {
				completion(nil, "there was an attempt...")
				return
			}
			
			// needs to explicitly deserialize because the nfl api returns week as an int
			// if you don't give it a week in the query
			guard let json = try? JSONSerialization.jsonObject(with: data),
				let result = json as? [String : Any],
				let week = result["week"] as? Int,
				let seasonString = result["season"] as? String,
				let season = Int(seasonString)
				else {
					completion(nil, "yeah, no...")
					return
			}

			completion((week, season), nil)
		}.resume()
	}
	
	private func isWeekComplete(_ week: Int, season: Int, completion: @escaping (Bool?, String?) -> ()) {
		let query = "players/weekstats?season=\(season)&week=\(week)"
		
		makeRequest(query: query) { (weekStats: NFLWeekStatistics?, error: String?) in
			guard let weekStats = weekStats else {
				completion(nil, error)
				return
			}
			
			completion(weekStats.complete, nil)
		}
	}
	
	private func makeRequest<T: Decodable>(query: String, completion: @escaping (T?, String?) -> ()) {
		guard let url = URL(string: baseAddress + query) else {
			completion(nil, "url was invalid")
			return
		}
		
		URLSession.shared.dataTask(with: url) { (data: Data?, response: URLResponse?, error: Error?) in
			guard let data = data, error == nil else {
				completion(nil, "there was an attempt...")
				return
			}
			
			guard let result = try? self.decoder.decode(T.self, from: data) else {
				completion(nil, "could not decode")
				return
			}
			
			completion(result, nil)
			
		}.resume()
	}
}