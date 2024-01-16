using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ship Controller
// Gabriel Staffen
/// <summary>
/// This class's only purpose is to keep track of ship health and return it to the main BattleshipGame script.
/// </summary>
public class ShipController : MonoBehaviour {
    // Hit Points
    int hitPoints;

    // Damage
    /// <summary>
    /// Public method for damaging the ship.
    /// </summary>
    /// <returns>Remaining hit points.</returns>
    public int Damage() {
        return --hitPoints;
    }

    // Setup
    /// <summary>
    /// Public method for setting up the hit points, position, and orientation for a ship.
    /// </summary>
    /// <param name="x">X grid position.</param>
    /// <param name="y">Y grid position.</param>
    /// <param name="hitPoints">The number of hit points the ship starts with.</param>
    /// <param name="vertical">If the ship is oriented vertically.</param>
    public void Setup(int x, int y, int hitPoints, bool vertical) {
        transform.localPosition = new Vector3(x, 0, -y);
        this.hitPoints = hitPoints;
        transform.localEulerAngles = new Vector3(0, vertical ? 0 : -90, 0);
    }
}
