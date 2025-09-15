using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform MainCharacter;
    public Transform HopBasket;
    private Transform _characterInstance;
    public Transform ShootingZone;
    private Transform[] _shootingZones;
    [SerializeField] int currentPosition = 0;
    // Start is called before the first frame update
    private void Awake()
    {
        int childCount = ShootingZone.childCount;
        _shootingZones = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            _shootingZones[i] = ShootingZone.GetChild(i);
        }
    }

    void Start()
    {
        _characterInstance = Instantiate(MainCharacter, _shootingZones[currentPosition].position, Quaternion.identity);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            currentPosition++;
            if (currentPosition >= _shootingZones.Length) currentPosition = 0;
            Vector3 newShootingZone = _shootingZones[currentPosition].position;
            _characterInstance.position = new Vector3(newShootingZone.x, 0f, newShootingZone.z);
            //Transform camera = _characterInstance.GetChild(0).transform.GetChild(0).transform;
            //camera.LookAt(HopBasket);
            Vector3 direction = HopBasket.position - _characterInstance.position;
            direction.y = 0;
            _characterInstance.rotation = Quaternion.LookRotation(-direction);
        }
    }
}
