using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Battleship Game
// Gabriel Staffen
/// <summary>
/// This class contains nearly everything necessary for playing the classic boardgame Battleship.
/// </summary>
public class BattleshipGame : MonoBehaviour {
    
    // Grid Square Struct
    /// <summary>
    /// Used to represent individual grid squares within the two grids of Battleship.
    /// </summary>
    struct Square {
        string name;        // The alpha-numerical name of the grid square (used primarily for debug purposes).
        bool occupied;      // If this grid square is occupied by a ship.
        bool hit;           // If this grid sqaure has previously been hit.
        bool playerGrid;    // If this grid square belongs to the player grid (rather than the opponent grid).
        int ship;           // The ship occupying the grid square (-1 if no ship is present).
        // Square Constructor
        /// <param name="name">Alpha-numerical name of the grid square.</param>
        /// <param name="playerGrid">If this grid square belongs to the player grid.</param>
        public Square(string name, bool playerGrid) {
            this.name = name;
            occupied = false;
            hit = false;
            this.playerGrid = playerGrid;
            ship = -1;
        }
        // Set Ship
        public void SetShip(int ship) {
            occupied = ship != -1;  // Reset occupied if ship is set to -1.
            this.ship = ship;
            if (ship == -1)
                hit = false;        // Reset hit if ship is set to -1.
        }
        // Get Ship
        public int GetShip() {
            return ship;
        }
        // Is Occupied
        public bool IsOccupied() {
            return occupied;
        }
        // Get Name
        public string GetName() {
            return name;
        }
        // Previously Hit
        public bool PreviouslyHit() {
            return hit;
        }
        // Hit
        public void Hit() {
            hit = true;
        }
    }

    // Game State
    /// <summary>
    /// The game state of Battleship is represented as a 3D array of grid square structs.
    /// Each grid is 10 by 10, with the z value representing which grid you are accessing (player or opponent).
    /// </summary>
    Square[,,] gameState = new Square[10, 10, 2];   // x, y, grid (0 = player, 1 = opponent)
    int[] remainingShips = { 5, 5 };                // Used as a quick way to keep track of remaining ships.
    int turn = 0;                                   // Turn counter. Maximum possible is 200.
    int winner = -1;                                // The winner of the game. (-1 = game in progress, 0 = player wins, 1 = opponent wins)

    // Player Setup
    PlayerSetup playerSetup = PlayerSetup.CLOSED;
    enum PlayerSetup {
        CLOSED = -2,    // The game board is closed (pre-setup).
        OPENING = -1,   // The game board is opening (animated, pre-setup).
        A = 0,          // Player needs to place their Aircraft Carrier.
        B = 1,          // Player needs to place their Battleship.
        D = 2,          // Player needs to place their Destroyer.
        S = 3,          // Player needs to place their Submarine.
        P = 4,          // Player needs to place their Patrol Boat.
        COMPLETE = 5,   // Player setup is complete.
        CLOSING = 6     // The game board is closing (animated, post-match, reset).
    }

    // Player Input
    int mouseGrid = -1;                     // The grid the mouse is currently over. (-1 = none, 0 = player, 1 = opponent)
    Vector2 mouseGridPosition;              // Local mouse position within the grid it was last over.
    bool placingVertically = true;          // If the object being placed by the player is being oriented vertically.

    // Grid GameObjects
    [SerializeField] GameObject[] gridObjects = new GameObject[2];  // (0 = player, 1 = opponent) These references are for instantiating ship gameObjects on the separate grids.

    // Ships & Prefabs
    // Ship Order: Aircraft Carrier, Battleship, Destroyer, Submarine, Patrol Boat.
    ShipController[,] ships = new ShipController[5, 2];         // ship, grid (0 = player, 1 = opponent)
    [SerializeField] GameObject[] prefabs = new GameObject[6];  // Ship Prefabs (0-4) + Pin Prefab (5).
    GameObject[] previewObjects = new GameObject[6];            // Ship Previews (0-4) + Pin Preview (5). Used to display a preview of the object being placed by the player.
    readonly int[] shipLengths = { 5, 4, 3, 3, 2 };             // The length of each ship.
    GameObject[] pins = new GameObject[200];                    // Pins placed during gameplay.
    int pinCount;                                               // The total number of pins placed. Could easily use turn number in place of this.

    // Materials
    [SerializeField] Material matWhite;         // White material for ships and pins.
    [SerializeField] Material matRed;           // Red material for ships and pins.
    [SerializeField] Material matPreview;       // Preview material for ships and pins.

    // Board Hinge
    [SerializeField] GameObject hinge;          // Used as a pivot point to open and possibly close the game board.
    float hingeInterpolation;                   // Interpolation of the hinge when opening (animation).
    [SerializeField] AnimationCurve hingeOpen;  // Animation curve of the hinge opening.
    [SerializeField] AnimationCurve hingeClose; // Animation curve of the hinge closing.

    // UI
    [SerializeField] GameObject setupHint;      // UI displayed when the player is placing their ships.
    [SerializeField] GameObject combatHint;     // UI displayed during the player's first turn.
    [SerializeField] GameObject restartButton;  // Button to restart/reset the match at any given time.
    [SerializeField] GameObject victoryScreen;  // UI displayed when the player wins.
    [SerializeField] GameObject defeatScreen;   // UI displayed when the opponent wins.

    // Start
    void Start() {
        // Generate an empty game state
        CreateGrids();
        // Create preview objects
        CreatePreviewObjects();
    }
    
    // Update
    void Update() {
        // Animate the game board opening.
        if (playerSetup == PlayerSetup.OPENING && hingeInterpolation < 1f) {
            hingeInterpolation = Mathf.Clamp(hingeInterpolation + Time.deltaTime * 0.65f, 0f, 1f);              // Increase the hinge interpolation with delta time, clamped between zero and one.
            hinge.transform.eulerAngles = new Vector3(hingeOpen.Evaluate(hingeInterpolation) * 105f, 0f, 0f);   // Apply the interpolation by evaluating an animation curve and multiplying it by 105 degrees.
            // If the opening animation is finished, advance the player setup.
            if (hingeInterpolation == 1f) {
                playerSetup = PlayerSetup.A;    // Start player ship placement.
                restartButton.SetActive(true);  // Display the restart button.
            }
        }

        // Animate the game board closing.
        if (playerSetup == PlayerSetup.CLOSING && hingeInterpolation > 0f) {
            hingeInterpolation = Mathf.Clamp(hingeInterpolation - Time.deltaTime * 0.8f, 0f, 1f);               // Decrease the hinge interpolation with delta time, clamped between zero and one.
            hinge.transform.eulerAngles = new Vector3(hingeClose.Evaluate(hingeInterpolation) * 105f, 0f, 0f);  // Apply the interpolation by evaluating an animation curve and multiplying it by 105 degrees.
            // If the closing animation is finished, reset the game.
            if (hingeInterpolation == 0f) {
                playerSetup = PlayerSetup.OPENING;  // Restart the player setup.
                ClearGrids();                       // Clear the game state.
                DestroyShips();                     // Destroy all ship gameObjects.
                DestroyPins();                      // Destroy all pin gameObjects.
                remainingShips[0] = 5;              // Reset player ships remaining to 5.
                remainingShips[1] = 5;              // Reset opponent ships remaining to 5.
                turn = 0;                           // Reset the turn counter.
                winner = -1;                        // Set the winner to nobody.
                PlaceShipsRandomly(1);              // Randomly generate the next opponent ship placement.
                placingVertically = true;           // Reset the player ship placement rotation.
            }
        }

        // Player Input
        if (playerSetup > PlayerSetup.OPENING) {    // Controls are only enabled after the opening animation has finished.
            mouseGrid = -1;                         // Always reset mouseGrid before each update raycast.
            // Raycast using the mouse position from the main camera. The layermask is limited to layers 6 and 7, which are the player and opponent grids respectively.
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 100, 3 << 6)) {
                mouseGrid = hit.collider.gameObject.layer - 6;                                              // Get the grid number from the layer.
                Vector3 pos = hit.transform.InverseTransformPoint(hit.point);                               // Convert the raycast hit world space point to the local space of the grid.
                mouseGridPosition = new Vector2(Mathf.Clamp(pos.x, 0f, 9f), Mathf.Clamp(-pos.z, 0f, 9f));   // Clamp, vertically flip, and rearrange the Vector3 position into a vector2.
            }

            // Player Setup
            if (playerSetup < PlayerSetup.COMPLETE) {
                // Rotate Ships
                if (Input.GetMouseButtonDown(1))    // Right clicking with the mouse will rotate ship placement.
                    placingVertically = !placingVertically;
                // Preview Ships
                ShowPreviewObject(mouseGrid == 0 ? (int)playerSetup : -1);
                if (mouseGrid == 0) {
                    // This is just a bit of rounding and clamping to center a ship of any length on the mouse position. (Ships pivot at their bow, not their center)
                    int x = Mathf.RoundToInt(Mathf.Clamp(mouseGridPosition.x - (placingVertically ? 0f : shipLengths[(int)playerSetup] * 0.5f - 0.5f), 0f, 10f - (placingVertically ? 1 : shipLengths[(int)playerSetup])));
                    int y = Mathf.RoundToInt(Mathf.Clamp(mouseGridPosition.y - (placingVertically ? shipLengths[(int)playerSetup] * 0.5f - 0.5f : 0f), 0f, 10f - (placingVertically ? shipLengths[(int)playerSetup] : 1)));
                    // Position and rotate the ship on the grid.
                    previewObjects[(int)playerSetup].GetComponent<ShipController>().Setup(x, y, 0, placingVertically);
                    // Place Ships
                    if (Input.GetMouseButtonDown(0) && ShipUnobstructed(x, y, 0, shipLengths[(int)playerSetup], placingVertically)) {   // Ship placement requires a obstruction check.
                        PlaceShip(x, y, 0, shipLengths[(int)playerSetup], (int)playerSetup, placingVertically);                         // Place ship.
                        playerSetup++;                                                                                                  // Advance the player setup.
                    }
                }
            }

            // Player Turns
            if (playerSetup == PlayerSetup.COMPLETE) {
                // Preview Pin
                ShowPreviewObject(mouseGrid == 1 && turn % 2 == 0 ? 5 : -1);
                if (mouseGrid == 1 && turn % 2 == 0 && winner == -1) {
                    // Round the mouse position to the grid.
                    int x = Mathf.RoundToInt(mouseGridPosition.x);
                    int y = Mathf.RoundToInt(mouseGridPosition.y);
                    // Position the pin on the grid.
                    previewObjects[5].transform.localPosition = new Vector3(x, 0, -y);
                    // Place Pin
                    if (Input.GetMouseButtonDown(0)) {
                        if (PlaceHit(x, y, 1) && winner == -1) {    // PlaceHit also returns a boolean regarding if the hit wasn't a repeat. (If it was, nothing is placed)
                            turn++;                                 // Increment the turn counter.
                            StartCoroutine(OpponentTurn());         // Start the opponent's turn with a delay.
                        }
                    }
                }
            }
        }

        // UI hints
        setupHint.SetActive(playerSetup > PlayerSetup.OPENING && playerSetup < PlayerSetup.COMPLETE);   // Display the ship placement hint during player ship placement.
        combatHint.SetActive(playerSetup == PlayerSetup.COMPLETE && turn == 0);                         // Display the combat hint during the player's first turn.
    }

    // Open Game
    /// <summary>
    /// Public method for starting the Battleship game by opening the game board.
    /// Also generates the opponent ship placement.
    /// </summary>
    public void OpenGame() {
        if (playerSetup == PlayerSetup.CLOSED) {
            playerSetup = PlayerSetup.OPENING;
            PlaceShipsRandomly(1);
        }
    }

    // Rematch
    /// <summary>
    /// Public method for restarting the game by closing the game board.
    /// </summary>
    public void Rematch() {
        playerSetup = PlayerSetup.CLOSING;
    }

    // Quit
    /// <summary>
    /// Public method for quitting the application.
    /// </summary>
    public void Quit() {
        Application.Quit();
    }
    
    // Create Grids
    /// <summary>
    /// Generates both empty grids for the game of Battleship.
    /// This is a critical step in initializing the game state.
    /// </summary>
    void CreateGrids() {
        for (int z = 0; z < 2; z++) {
            for (int y = 0; y < 10; y++) {
                for (int x = 0; x < 10; x++) {
                    gameState[x, y, z] = new Square(SquareName(x, y), z == 0);
                }
            }
        }
    }
    
    // Square Name
    /// <summary>
    /// Generates the alpha-numerical name for any given grid square coordinates.
    /// </summary>
    /// <param name="x">X grid position.</param>
    /// <param name="y">Y grid position.</param>
    /// <returns>The alpha-numerical name for the entered grid square coordinates.</returns>
    string SquareName(int x, int y) {
        string rows = "ABCDEFGHIJ";
        return rows.Substring(y, 1) + (x + 1).ToString();
    }

    // Create Preview Objects
    /// <summary>
    /// Instantiates the gameObjects required to preview any player actions.
    /// Preview objects use the same prefabs but are disconnected from the game state and given a transparent material.
    /// </summary>
    void CreatePreviewObjects() {
        for (int i = 0; i < 6; i++) {
            previewObjects[i] = Instantiate(prefabs[i], i < 5 ? gridObjects[0].transform : gridObjects[1].transform);
            previewObjects[i].transform.Find("Model").GetComponent<MeshRenderer>().material = matPreview;
            previewObjects[i].SetActive(false);
        }
    }

    // Show Preview Object
    /// <summary>
    /// Activates a given preview object while deactivating all others.
    /// If a value out of range is given, all preview objects will be deactivated.
    /// </summary>
    /// <param name="previewObject">The index of a given preview object.</param>
    void ShowPreviewObject(int previewObject) {
        for (int i = 0; i < 6; i++) {
            previewObjects[i].SetActive(i == previewObject);
        }
    }

    // Destroy Ships
    /// <summary>
    /// Destroys all ship gameObjects that were in play.
    /// This is for the purposes of reseting the game state.
    /// </summary>
    void DestroyShips() {
        for (int i = 0; i < 2; i++) {
            for (int j = 0; j < 5; j++) {
                if (ships[j, i] != null)
                    Destroy(ships[j, i].gameObject);
            }
        }
    }

    // Destroy Pins
    /// <summary>
    /// Destroy all pin gameObjects that were in play.
    /// This is for the purposes of reseting the game state.
    /// </summary>
    void DestroyPins() {
        for (int i = 0; i < pinCount; i++) {
            Destroy(pins[i]);
        }
        pinCount = 0;
    }

    // Clear Grids
    /// <summary>
    /// Clears the game state of any placed ships.
    /// </summary>
    void ClearGrids() {
        for (int z = 0; z < 2; z++) {
            for (int y = 0; y < 10; y++) {
                for (int x = 0; x < 10; x++) {
                    gameState[x, y, z].SetShip(-1);
                }
            }
        }
    }

    // Place Ships Randomly
    /// <summary>
    /// Places all ships for a given grid in a random legal arrangement.
    /// If used to generate the opponent's fleet, the ships will have their models deactivated.
    /// </summary>
    /// <param name="grid">The grid on which all ships are generated.</param>
    void PlaceShipsRandomly(int grid) {
        for (int i = 0; i < 5; i++) {
            bool placed = false;
            bool vertical;
            int x, y;
            // Repeat with random orientations and positions until a legal placement is found.
            while (!placed) {
                vertical = Random.Range(0,2) == 0;
                x = Random.Range(0, 11 - (vertical ? 1 : shipLengths[i]));
                y = Random.Range(0, 11 - (vertical ? shipLengths[i] : 1));
                if (ShipUnobstructed(x, y, grid, shipLengths[i], vertical)) {   // Ship placement requires a obstruction check.
                    PlaceShip(x, y, grid, shipLengths[i], i, vertical);         // Place ship.
                    placed = true;
                    Debug.Log("Placed ship " + i + " at square " + gameState[x, y, grid].GetName() + " oriented " + (vertical ? "vertically." : "horizontally."));
                }
            }
        }
        Debug.Log("Successfully placed all " + (grid == 0 ? "player" : "opponent") + " ships randomly");
    }

    // Ship Unobstructed
    /// <summary>
    /// Checks if any ships obstruct the placement of a ship of a given length and orientation.
    /// </summary>
    /// <param name="x">X grid position.</param>
    /// <param name="y">Y grid position.</param>
    /// <param name="grid">The grid on which the check is performed.</param>
    /// <param name="length">The length of the ship.</param>
    /// <param name="vertical">If the ship is oriented vertically.</param>
    /// <returns>If the ship is unobstructed.</returns>
    bool ShipUnobstructed(int x, int y, int grid, int length, bool vertical) {
        for (int i = 0; i < length; i++) {
            if (gameState[x + (vertical ? 0 : i), y + (vertical ? i : 0), grid].IsOccupied())
                return false;
        }
        return true;
    }

    // Place Ship
    /// <summary>
    /// Places a ship on a given grid at a given position and orientation.
    /// Opponent ships have their models deactivated to prevent the player from seeing their location until they are destroyed.
    /// </summary>
    /// <param name="x">X grid position.</param>
    /// <param name="y">Y grid position.</param>
    /// <param name="grid">The grid on which the ship will be placed.</param>
    /// <param name="length">The length of the ship.</param>
    /// <param name="ship">The ship index. This is used when applying damage.</param>
    /// <param name="vertical">If the ship is oriented vertically.</param>
    void PlaceShip(int x, int y, int grid, int length, int ship, bool vertical) {
        ships[ship, grid] = Instantiate(prefabs[ship], gridObjects[grid].transform).GetComponent<ShipController>();
        ships[ship, grid].Setup(x, y, length, vertical);
        if (grid == 1)
            ships[ship, grid].transform.Find("Model").gameObject.SetActive(false);
        for (int i = 0; i < length; i++) {
            gameState[x + (vertical ? 0 : i), y + (vertical ? i : 0), grid].SetShip(ship);
        }
    }

    // Place Hit
    /// <summary>
    /// Places a hit on a given grid square if the square is yet to be hit.
    /// If the hit is a repeat the method will return false.
    /// Win/Lose conditions are determined in this method if this is the final blow to the last remaining ship of a given grid.
    /// </summary>
    /// <param name="x">X grid position.</param>
    /// <param name="y">Y grid position.</param>
    /// <param name="grid">The grid being hit.</param>
    /// <returns>If the hit was successful and not a repeat of a previous turn.</returns>
    bool PlaceHit(int x, int y, int grid) {
        Square square = gameState[x, y, grid];
        if (!square.PreviouslyHit()) {                                                                                      // Check if this is a repeat turn.
            if (square.IsOccupied()) {                                                                                      // Check if a ship was hit.
                if (ships[square.GetShip(), grid].Damage() == 0) {                                                          // Check if the ship has been destroyed.
                    ships[square.GetShip(), grid].transform.Find("Model").gameObject.SetActive(true);                       // Make destroyed ships visible.
                    ships[square.GetShip(), grid].transform.Find("Model").GetComponent<MeshRenderer>().material = matRed;   // Make destroyed ships red.
                    remainingShips[grid]--;                                                                                 // Decrement remaining ships.
                    if (remainingShips[grid] == 0) {                                                                        // Check if the entire fleet is destroyed.
                        // Win/Lose State
                        winner = (grid + 1) % 2;
                        Debug.Log((winner == 0 ? "Player" : "Opponent") + " Wins!");
                        restartButton.SetActive(false);     // Hide restart button (redundancy).
                        if (winner == 0)
                            victoryScreen.SetActive(true);  // Player Victory
                        if (winner == 1)
                            defeatScreen.SetActive(true);   // Player Defeat
                    }
                }
            }
            pins[pinCount] = Instantiate(prefabs[5], gridObjects[grid].transform);                                                      // Instantiate a hit pin.
            pins[pinCount].transform.localPosition = new Vector3(x, 0, -y);                                                             // Position the pin on the grid.
            pins[pinCount++].transform.Find("Model").GetComponent<MeshRenderer>().material = square.IsOccupied() ? matRed : matWhite;   // Color the pin red if it hit a ship.
            gameState[x, y, grid].Hit();                                                                                                // Add the hit to the game state.
            return true;
        }
        return false;
    }

    // Opponent Turn
    /// <summary>
    /// The opponent will take a random turn after a 0.65 second delay.
    /// </summary>
    IEnumerator OpponentTurn() {
        yield return new WaitForSeconds(0.65f);
        bool hasGone = false;
        int x, y;
        // Repeat with random moves until a new move is found.
        while (!hasGone) {
            x = Random.Range(0, 10);
            y = Random.Range(0, 10);
            hasGone = PlaceHit(x, y, 0);
        }
        turn++; // Increment turn counter.
    }
}
