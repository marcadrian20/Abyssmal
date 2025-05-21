# Enemy AI System

## Overview
The Enemy AI System is designed to provide a flexible and extensible framework for creating various types of enemies in a game. This system includes support for melee, ranged, and flying enemies, as well as patrol behavior using pathfinding.

## Project Structure
The project is organized into the following directories:

- **Scripts/**: Contains all the scripts related to enemy AI and behavior.
  - **Enemies/**: Contains the core enemy AI classes.
    - `EnemyAI.cs`: Abstract class defining the basic structure for enemy AI.
    - `MeleeEnemyAI.cs`: Implements melee attack logic and patrol behavior.
    - `RangedEnemyAI.cs`: Implements ranged attack logic, including projectile firing.
    - `FlyingEnemyAI.cs`: Implements flying behavior and aerial attacks.
    - `PatrolRoute.cs`: Defines patrol routes for enemies.
    - `EnemyCombat.cs`: Abstract class for enemy combat behavior.
  - **Projectiles/**: Contains scripts related to projectiles.
    - `EnemyProjectile.cs`: Handles projectile behavior for ranged enemies.
  - **Utils/**: Contains utility scripts.
    - `PathfindingHelper.cs`: Provides utility functions for pathfinding.

## Setup Instructions
1. Clone the repository to your local machine.
2. Open the project in your preferred development environment.
3. Ensure all dependencies are installed.
4. Customize enemy behavior by modifying the respective classes in the `Scripts/Enemies/` directory.

## Usage
- To create a new enemy type, inherit from the `EnemyAI` class and implement the required methods.
- Use the `PatrolRoute` class to define patrol paths for your enemies.
- For ranged enemies, utilize the `EnemyProjectile` class to manage projectile behavior.

## Contributing
Contributions are welcome! Please submit a pull request or open an issue for any enhancements or bug fixes.

## License
This project is licensed under the MIT License. See the LICENSE file for more details.