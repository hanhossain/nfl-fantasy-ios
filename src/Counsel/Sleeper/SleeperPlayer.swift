//
//  SleeperPlayer.swift
//  Counsel
//
//  Created by Han Hossain on 9/29/19.
//  Copyright © 2019 Han Hossain. All rights reserved.
//

import Foundation

struct SleeperPlayer: Codable {
    let firstName: String
    let lastName: String
    let position: String?
    let active: Bool
    let id: String
    let team: String?
    
    enum CodingKeys: String, CodingKey {
        case firstName = "first_name"
        case lastName = "last_name"
        case position = "position"
        case active = "active"
        case id = "player_id"
        case team = "team"
    }
}
