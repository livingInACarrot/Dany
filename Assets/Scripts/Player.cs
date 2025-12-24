using UnityEngine;

public class Player : MonoBehaviour
{
    private int id;
    public string Name { get; protected set; }

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void SetName(string name)
    {
        Name = name;
    }
}
