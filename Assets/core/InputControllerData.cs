using UnityEngine;

[RequireComponent(typeof(CharacterMotor))]
[AddComponentMenu("Character/FPS Input Controller")]

public class InputControllerData : MonoBehaviour
{
    public float mov;
    public float z;

    // Update is called once per frame
    void Update()
    {
        mov = Input.GetAxis("Horizontal");
        z = Input.GetAxis("Vertical");
    }
}
