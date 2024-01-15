using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {
    
    struct Square {
        string name;
        bool occupied, hit, playerBoard;
        int ship;
        public Square(string name, bool playerBoard) {
            this.name = name;
            occupied = false;
            hit = false;
            this.playerBoard = playerBoard;
            ship = -1;
        }
        public void SetShip(int ship) {
            occupied = true;
            this.ship = ship;
        }
    }
    Square[,,] gameBoards = new Square[10, 10, 2];
    
    struct Ship {
        int hp;
        bool playerShip, placed;
        public Ship(int hp, bool playerShip) {
            this.hp = hp;
            this.playerShip = playerShip;
            placed = false;
        }
    }
    Ship[,] ships = new Ship[5, 2];
    GameObject[,] shipObjects = new GameObject[5, 2];
    readonly int[] shipLengths = { 5, 4, 3, 3, 2 };

    void Start() {
        CreateGameBoards();
        CreateShips();
    }
    
    void Update() {
        
    }
    
    void CreateGameBoards() {
        for (int z = 0; z < 2; z++) {
            for (int y = 0; y < 10; y++) {
                for (int x = 0; x < 10; x++) {
                    gameBoards[x, y, z] = new Square(SquareName(x, y), z == 0);
                }
            }
        }
    }
    
    string SquareName(int x, int y) {
        string rows = "ABCDEFGHIJ";
        return rows.Substring(y, 1) + (x + 1).ToString();
    }

    void CreateShips() {
        for (int i = 0; i < 2; i++) {
            for (int j = 0; j < 5; j++) {
                ships[j, i] = new Ship(shipLengths[j], i == 0);
            }
        }
    }

    void PopulateEnemyBoardRandomly() {

    }
}
