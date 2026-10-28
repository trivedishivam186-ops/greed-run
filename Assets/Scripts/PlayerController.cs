using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the player around the arena.
/// PC: WASD or arrow keys.
/// Phone (and mouse): touch anywhere and drag - the robot moves in that direction.
/// The further you drag, the faster it goes, up to full speed.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement (designer knobs)")]
    [Tooltip("Top speed, in units per second.")]
    public float moveSpeed = 7f;

    [Tooltip("How fast the robot turns to face where it's going (degrees per second).")]
    public float turnSpeed = 720f;

    [Tooltip("The player can't go further than this from the centre. The outer neon edge is at 28.")]
    public float arenaHalfSize = 27f;

    [Header("Touch / drag control")]
    [Tooltip("How far you drag for full speed, as a share of screen height (0.08 = 8% of the screen).")]
    [Range(0.02f, 0.3f)]
    public float dragForFullSpeed = 0.08f;

    [Tooltip("Tiny drags smaller than this are ignored, so a light touch doesn't move you (share of screen height).")]
    [Range(0f, 0.05f)]
    public float deadZone = 0.01f;

    // Other scripts can read these later (e.g. to draw an on-screen joystick).
    public bool IsDragging { get; private set; }
    public Vector2 DragStart { get; private set; }
    public Vector2 DragCurrent { get; private set; }
    public Vector3 MoveDirection { get; private set; }

    void Update()
    {
        // Keyboard first; if no keys are pressed, check for a touch/mouse drag.
        Vector2 input = ReadKeyboard();
        if (input == Vector2.zero)
            input = ReadDrag();

        // Turn the 2D input into movement on the floor (X = left/right, Z = up/down the screen).
        Vector3 move = new Vector3(input.x, 0f, input.y);
        MoveDirection = move;

        transform.position += move * moveSpeed * Time.deltaTime;

        // Keep the player inside the arena.
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, -arenaHalfSize, arenaHalfSize);
        p.z = Mathf.Clamp(p.z, -arenaHalfSize, arenaHalfSize);
        transform.position = p;

        // Smoothly turn to face the direction of movement.
        if (move.sqrMagnitude > 0.0001f)
        {
            Quaternion target = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }

    Vector2 ReadKeyboard()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return Vector2.zero;

        Vector2 v = Vector2.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
        return v.normalized; // so diagonals aren't faster
    }

    Vector2 ReadDrag()
    {
        // "Pointer" covers both a finger on a phone and a mouse on a PC.
        Pointer pointer = Pointer.current;
        if (pointer == null || !pointer.press.isPressed)
        {
            IsDragging = false;
            return Vector2.zero;
        }

        Vector2 pos = pointer.position.ReadValue();

        // The moment the finger goes down, remember where it started.
        if (!IsDragging)
        {
            IsDragging = true;
            DragStart = pos;
        }
        DragCurrent = pos;

        Vector2 delta = pos - DragStart;
        float screenH = Mathf.Max(1f, Screen.height);
        float dist = delta.magnitude / screenH;       // drag distance as a share of screen height

        if (dist < deadZone) return Vector2.zero;

        float strength = Mathf.Clamp01((dist - deadZone) / Mathf.Max(0.001f, dragForFullSpeed - deadZone));
        return delta.normalized * strength;
    }
}
