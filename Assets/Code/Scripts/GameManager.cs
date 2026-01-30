using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//TODO: Fix camera position after shot
//TODO: Fix balls collision between each other
//TODO: Fix ball collision outside map (respawn) -> Consider using Layers

public class GameManager : MonoBehaviour
{
    public Transform HoopBasket;
    public Transform CameraTarget;
    public Transform Backboard;
    public Transform ShootingZone;
    public Transform EndGameZoneEmpty;
    [SerializeField] private Transform mainCharacter;
    [SerializeField] private Transform opponentCharacter;
    [SerializeField] int currentPositionPlayer = 0;
    [SerializeField] private GameObject[] balls;
    private Transform _characterInstance;
    private Transform _characterHand;
    private Transform[] _shootingZones;
    private Transform[] _endGameZones;
    private BallController _ballInstance;
    private BallController _opponentBallInstance;

    public int TotalScore { get;  private set; }

    // UI
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshPro scoreText;
    [SerializeField] private Button endGameButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button menuButton;

    // Game State
    public GameState State;
    public static event Action<GameState> OnGameStateChanged;

    // Game mode
    public bool IsSinglePlayer;
    public Vector3 OpponentPositionOffset = new Vector3(-0.69f, 0, 0.27f);
    public int CurrentPositionOpponent = 0;
    [SerializeField] private TextMeshProUGUI opponentScoreText;
    public int OpponentScore { get; private set; }
    private Transform _opponentInstance;

    // Audio
    public AudioSource SFXManager;
    public AudioSource ThemeManager;

    // Particles
    [SerializeField] private GameObject[] shotParticles;

    public static GameManager Instance { get; private set; }

    public static bool GameIsOver() => Instance != null && Instance.State == GameState.GameOver;

    Animator characterAnimator;

    private int _lastBallsShotCounter;

    // --- Decoupling: assign these adapter components in Inspector ---
    [Header("Service Adapters (assign adapter components)")]
    [SerializeField] private InputManagerAdapter inputServiceBehaviour;
    [SerializeField] private CameraControllerAdapter cameraServiceBehaviour;
    [SerializeField] private FireballControllerAdapter fireballServiceBehaviour;
    [SerializeField] private SceneControllerAdapter sceneServiceBehaviour;

    private IInputService InputService => inputServiceBehaviour as IInputService;
    private ICameraService CameraService => cameraServiceBehaviour as ICameraService;
    private IFireballService FireballService => fireballServiceBehaviour as IFireballService;
    private ISceneService SceneService => sceneServiceBehaviour as ISceneService;

    private void Awake()
    {
        // Prevent class instance duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        
        UpdateGameState(GameState.Startup);

        SetupZones();

        // Check if services are null
        if (InputService == null) throw new NullReferenceException("InputService not found!");
        if (CameraService == null) throw new NullReferenceException("CameraService not found!");
        if (FireballService == null) throw new NullReferenceException("FireballService not found!");
        if (SceneService == null) throw new NullReferenceException("SceneService not found!!!");

        endGameButton.onClick.AddListener(() => ResetBall(true)); //SceneService.BackToMainMenu());
        retryButton.onClick.AddListener(() => SceneService.StartGame());
        menuButton.onClick.AddListener(() => SceneService.BackToMainMenu());
        
        // To know if players shot last balls on timer end
        _lastBallsShotCounter = 0;
    }

    private void OnDestroy()
    {
        if (_ballInstance != null) _ballInstance.LastBallShot -= LastBallsShot;
        if (_opponentBallInstance != null) _opponentBallInstance.LastBallShot -= LastBallsShot;
    }

    public void ResetBall(bool aiState = false)
    {
        ResetGameState(aiState);
        _opponentBallInstance.ResetState();
    }

    private void SetupZones()
    {
        // Get shooting zones
        int childCount = ShootingZone.childCount;
        _shootingZones = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            _shootingZones[i] = ShootingZone.GetChild(i);
        }

        // Setup zones where to place players and camera at the end of the match
        childCount = EndGameZoneEmpty.childCount; // Reset value
        _endGameZones = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            _endGameZones[i] = EndGameZoneEmpty.GetChild(i);
        }
    }

    public IEnumerator SpawnShotParticles(Vector3 shotPos, int shotType)
    {
        if (shotType > (shotParticles.Length - 1)) throw new ArgumentOutOfRangeException("Particle out of list!");

        GameObject shotParticle = Instantiate(shotParticles[shotType], shotPos, Quaternion.identity);
        yield return new WaitForSeconds(3);
        Destroy(shotParticle);
    }

    private void Start()
    {
        SpawnCharacter();
        int[] totalScores = SceneService != null ? SceneService.GetScores() : new int[] { 0, 0 };
        TotalScore = totalScores[0];
        totalScoreText.text = string.Format("Score: {0}", TotalScore);
        if (!IsSinglePlayer) {
            OpponentScore = totalScores[1];
            opponentScoreText.text = string.Format("AI Score: {0}", OpponentScore);
            SpawnOpponent(); 
        }
        opponentScoreText.gameObject.SetActive(!IsSinglePlayer);
    }

    void RecalculatePlayerShootingZone()
    {
        // Apply config if present in the shooting zone
        var provider = _shootingZones[currentPositionPlayer].GetComponent<ShootingZoneConfigProvider>();
        if (provider != null)
        {
            _ballInstance.ApplyZoneConfig(provider.Config, provider.BackboardTarget);
            Debug.Log("Player BackboardTarget: " + "X: " + provider.BackboardTarget.position.x + ",Y: " + provider.BackboardTarget.position.y + ",Z: " + provider.BackboardTarget.position.z);
        }
    }

    void RecalculateOpponentShootingZone()
    {
        // Apply config if present in the shooting zone
        var oppProvider = _shootingZones[CurrentPositionOpponent].GetComponent<ShootingZoneConfigProvider>();
        if (oppProvider != null)
        {
            _opponentBallInstance.ApplyZoneConfig(oppProvider.Config, oppProvider.BackboardTarget);
        }
    }

    // Spawn main player
    void SpawnCharacter()
    {
        _characterInstance = Instantiate(mainCharacter, _shootingZones[currentPositionPlayer].position, Quaternion.Euler(0, 180f, 0));
        if (_characterInstance)
        {
            CameraService?.SetupPlayerCamera(_characterInstance.GetChild(1).transform, CameraTarget);
            characterAnimator = _characterInstance.GetComponentInChildren<Animator>();
            _characterHand = characterAnimator.GetBoneTransform(HumanBodyBones.RightHand).GetChild(5);

            _ballInstance = Instantiate(balls[0], _characterHand.position, Quaternion.identity).GetComponent<BallController>();

            RecalculatePlayerShootingZone();

            _ballInstance.BallStart = _characterHand;
            _ballInstance.ResetState();
            // Subscribe ball controller to last ball shot event
            _ballInstance.LastBallShot += LastBallsShot;
        }
        else
        {
            throw new NullReferenceException("Character instance not found!");
        }
    }

    // TODO: Fix initial rotation towards hoop
    void SpawnOpponent()
    {
        _opponentInstance = Instantiate(opponentCharacter, _shootingZones[CurrentPositionOpponent].position + OpponentPositionOffset, Quaternion.Euler(0, 180f, 0));
        if (_opponentInstance)
        {
            _opponentBallInstance = Instantiate(balls[1], _opponentInstance.GetComponent<AIController>().OpponentHand.position, Quaternion.identity).GetComponent<BallController>();

            RecalculateOpponentShootingZone();

            // Subscribe to last ball shot event
            _opponentBallInstance.LastBallShot += LastBallsShot;

            _opponentInstance.GetComponent<AIController>().BallInstance = _opponentBallInstance;
            _opponentBallInstance.AIBall = true; // Set AI ball
            _opponentBallInstance.Opponent = _opponentInstance.GetComponent<AIController>();
            _opponentBallInstance.BallStart = _opponentInstance.GetComponent<AIController>().OpponentHand;
            _opponentBallInstance.ResetState();
        }
        else
        {
            throw new NullReferenceException("Opponent instance not found!");
        }
    }

    void LastBallsShot()
    {
        _lastBallsShotCounter++;
    }

    public bool LastBallsAreShot()
    {
        return _lastBallsShotCounter >= 2;
    }

    // Called on first shot setup and in next ones, from both player and opponent
    void UpdatePosition(ref int currentPosition, Transform playerInstance, Vector3 offset)
    {
        if (currentPosition >= _shootingZones.Length) currentPosition = 0;
        Vector3 newShootingZone = _shootingZones[currentPosition].position;
        Vector3 targetPos = new Vector3(newShootingZone.x, 0f, newShootingZone.z) + offset;

        // Get animator and save previous state
        Animator anim = playerInstance.GetComponentInChildren<Animator>();
        bool prevAnimEnabled = false;
        bool prevApplyRoot = false;
        if (anim != null)
        {
            prevAnimEnabled = anim.enabled;
            prevApplyRoot = anim.applyRootMotion;
            // Temporarily disable animator/root motion and reset internal state
            anim.enabled = false;
            anim.applyRootMotion = false;
            anim.Rebind();
            anim.Update(0f);
        }

        // Snap position + rotation
        playerInstance.position = targetPos;
        Vector3 direction = HoopBasket.position - playerInstance.position;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.0001f)
            playerInstance.rotation = Quaternion.LookRotation(direction);

        // Clear physics impulses so the character doesn't drift after teleport
        ClearPhysicsOnTransform(playerInstance);

        // Restore animator and force correct state (prevent immediate root motion move)
        if (anim != null)
        {
            anim.enabled = prevAnimEnabled;
            anim.applyRootMotion = prevApplyRoot;

            // Reset animator parameters to safe defaults to avoid unexpected translation.
            // Adjust these names to match your Animator parameters exactly.
            try
            {
                anim.SetBool("shoot", false);
                //anim.SetBool("dribble", true);
                anim.Update(0f); // apply immediately
            }
            catch (Exception)
            {
                // in case parameters don't exist, ignore
            }
        }
    }

    // Zero velocities and reset character controllers on a transform (and its children)
    private void ClearPhysicsOnTransform(Transform t)
    {
        // Zero rigidbodies (temporarily make kinematic to ensure snap)
        var rbs = t.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs)
        {
            bool wasKinematic = rb.isKinematic;
            // Temporarily make kinematic to snap without physics impulses
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // restore kinematic state
            rb.isKinematic = wasKinematic;
        }

        // Reset CharacterController(s)
        var controllers = t.GetComponentsInChildren<CharacterController>();
        foreach (var cc in controllers)
        {
            cc.enabled = false;
            cc.enabled = true;
        }

        // Reset NavMeshAgent(s) if any (use full name to avoid adding using)
        var agents = t.GetComponentsInChildren<UnityEngine.AI.NavMeshAgent>();
        foreach (var ag in agents)
        {
            ag.isStopped = true;
            ag.velocity = Vector3.zero;
            ag.ResetPath();
            ag.isStopped = false;
        }
    }

    // Compute shot based on input strength
    public void OnBallShot(float shootingSpeed)
    {
        if (!_ballInstance) return;

        characterAnimator.SetBool("shoot", true);
        _ballInstance.PrepareShot(_characterHand);
        StartCoroutine(ShootBall(shootingSpeed));
    }

    private IEnumerator ShootBall(float shootingSpeed)
    {
        yield return new WaitForSeconds(0.7f);

        if (!_ballInstance) yield return null;

        _ballInstance.Shoot(shootingSpeed);
        CameraService?.StartMoving();
    }

    // Reset game stats for next shot, for AI or player
    public void ResetGameState(bool aiState = false)
    {
        if (!aiState)
        {
            scoreText.gameObject.SetActive(false);
            UpdatePosition(ref currentPositionPlayer, _characterInstance, Vector3.zero);
            // Reset camera position for next shot
            CameraService?.SetupPlayerCamera(_characterInstance.GetChild(1).transform, CameraTarget);
            InputService?.RestartShot();
            characterAnimator.SetBool("shoot", false);

            RecalculatePlayerShootingZone();
        } else
        {
            UpdatePosition(ref CurrentPositionOpponent, _opponentInstance, OpponentPositionOffset);

            RecalculateOpponentShootingZone();
        }
    }

    // Called on shot succeeded, for AI and player
    public void Win(int points, bool aiWon)
    {
        if (aiWon)
        {
            OpponentScore += points;
            opponentScoreText.text = string.Format("AI Score: {0}", OpponentScore);
            CurrentPositionOpponent++;  // Update opponent position for next shot
        } else
        {
            int multiplier = FireballService != null ? FireballService.FireballMultiplier : 1;
            points *= multiplier;
            scoreText.text = string.Format("+{0} points!", points);  // Show single score UI (only player)
            scoreText.gameObject.SetActive(true);
            TotalScore += points;
            totalScoreText.text = string.Format("Score: {0}", TotalScore);
            currentPositionPlayer++; // Update player position for next shot

            // Manage fireOn mode
            FireballService?.AddScore((float)points / 8);
        }
    }

    public void PlayerWins()
    {
        CurrentPositionOpponent++;
        ResetBall(true);
    }

    public void Lose(bool aiLost)
    {
        if (aiLost) return;
        FireballService?.OnMissedShot();   // Set Fireball counter to zero if 1 shot missed
        ResetGameState();   // Reset player state for next shot
    }

    // Activate idle animations and remove balls on game end
    public void GameEnded(bool evenPoints)
    {
        if (evenPoints) return;

        characterAnimator.SetBool("game_ended", true);
        _opponentInstance.GetComponent<AIController>()?.GameOver();

        //_ballInstance.GameOver();
        //_opponentBallInstance.GameOver();
        Destroy(_ballInstance != null ? _ballInstance.gameObject : null);
        Destroy(_opponentBallInstance != null ? _opponentBallInstance.gameObject : null);
    }

    public void Victory(bool mainCharWins)
    {
        MoveCharactersToPodiums(mainCharWins);
        characterAnimator.SetTrigger(mainCharWins ? "victory" : "defeat");
        // Opposite of main player
        _opponentInstance.GetComponent<AIController>()?.Victory(!mainCharWins);
    }

    private void MoveCharactersToPodiums(bool playerWon)
    {
        EndGameZone mainCharZone = playerWon ? EndGameZone.WinnerZone : EndGameZone.LoserZone;
        EndGameZone opponentZone = playerWon ? EndGameZone.LoserZone : EndGameZone.WinnerZone;
        _characterInstance.SetParent(_endGameZones[(int)mainCharZone]);
        _opponentInstance.SetParent(_endGameZones[(int)opponentZone]);
        ResetCharactersPositions();
        CameraService?.SetPodiumCamera(_endGameZones[(int)EndGameZone.CameraZone]);
    }

    // Reset characters positions when they are in the podium so they rotate towards the camera
    private void ResetCharactersPositions()
    {
        _characterInstance.transform.localPosition = Vector3.zero;
        _characterInstance.transform.localRotation = Quaternion.identity;

        _opponentInstance.transform.localPosition = Vector3.zero;
        _opponentInstance.transform.localRotation = Quaternion.identity;
    }

    // Manage game states
    public void UpdateGameState(GameState newState)
    {
        State = newState;
        if (InputService != null)
            InputService.Enabled = State == GameState.Play;
        else
            InputManager.Instance.enabled = State == GameState.Play; // fallback to existing singleton if adapter not assigned

        OnGameStateChanged?.Invoke(newState);
    }

    public enum GameState
    {
        Startup,
        Play,
        Pause,
        GameOver
    }

    public enum EndGameZone
    {
        WinnerZone,
        LoserZone,
        CameraZone
    }
}
