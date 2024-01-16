using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Pin Controller
// Gabriel Staffen
/// <summary>
/// This class's only purpose is to animate the pins dropped onto the Battleship grids during play.
/// </summary>
public class PinController : MonoBehaviour {
    // Y Offset
    float yOffset = 8f;

    // Update
    void Update() {
        yOffset = Mathf.Clamp(yOffset - Time.deltaTime * 12f, 0f, 5f);                                          // Decrease the Y offset by delta time.
        transform.localPosition = new Vector3(transform.localPosition.x, yOffset, transform.localPosition.z);   // Apply the Y offset to the local position.
    }
}
