using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    const float CAMERA_ZOOM_IN_DEPTH = 5.0f;

    public static event Action LevelCompleted;

    [SerializeField]
    private List<AudioClip> playerSFXs;
    [SerializeField] 
    private AudioSource playerSFXAudioSource;
    [SerializeField]
    private ParticleSystem levelCompleteVFX;
    [SerializeField]
    private List<ParticleSystem> mazeTransitionVFX;
    [SerializeField]
    private GameObject playerCamera;
    [SerializeField]
    private GameObject playerMesh;
    [SerializeField]
    private float movementForce = 3.0f;
    
    private bool moving = false;
    private bool GameOver = false;
    private bool GamePaused = false;
    private bool BirdsEyeViewActive = true;
    
    private string currDirection = string.Empty;
    private float counter = 0;
    
    private Vector3 newPlayerPosition = Vector3.zero;
    private Animator playerAnimator;
    private Coroutine centerCameraCR;
    private Coroutine stopSmokeCR;
    private InputAction movement;
    private PlayerControls playerInputs;
    private Rigidbody rb;
    private LevelManager levelManager;

    public void GamePausedState(bool state)
    {
        GamePaused = state;
    }

    public void GameOverState(bool state)
    {
        GameOver = state;
        playerSFXAudioSource.Stop();
        playerSFXAudioSource.PlayOneShot(playerSFXs.Find(clip => clip.name == SFXClipMap.SFX_CLIP_NAME_LEVEL_FAILED));
    }

    public void StartLevel()
    {
        mazeTransitionVFX.ForEach(vfx => vfx.Stop());
        playerCamera.GetComponent<Camera>().orthographicSize = MazeManager.mazeCenter[1];
        playerCamera.gameObject.transform.position = new Vector3(MazeManager.mazeCenter[0], playerCamera.gameObject.transform.position.y, MazeManager.mazeCenter[1]);
        centerCameraCR = StartCoroutine(CenterCamera(new Vector3(transform.position.x, playerCamera.transform.position.y, transform.position.z), CAMERA_ZOOM_IN_DEPTH, false, 3.0f));
    }

    private void Awake()
    {
        playerInputs = new PlayerControls();
    }

    private void OnEnable()
    {
        movement = playerInputs.Player.Movement;
        movement.Enable();

        playerInputs.Player.AxisChange.started += ChangeAxis;
        playerInputs.Player.BirdsEye.started += ActivateBirdsEye;
        playerInputs.Player.BirdsEye.canceled += DeactivateBirdsEye;

        playerInputs.Player.AxisChange.Enable();
        playerInputs.Player.BirdsEye.Enable();
    }

    private void OnDisable()
    {
        movement.Disable();

        playerInputs.Player.AxisChange.started -= ChangeAxis;
        playerInputs.Player.AxisChange.Disable();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
        levelManager = GameObject.Find("LevelManager").GetComponent<LevelManager>();
    }

    void Update()
    {
        if(!moving && movement.IsPressed() && Time.time > (counter + 0.25f) && !GameOver && !GamePaused && !BirdsEyeViewActive)
        {
            counter = Time.time + 0.25f;
            moving = true;
            AttemptMove(InputUtil.GetInputDirection(movement));
        }
    }

    private void FixedUpdate()
    {
        float currX = (currDirection == InputUtil.INPUT_DIRECTION_RIGHT) ? Mathf.Floor(transform.position.x) : Mathf.Ceil(transform.position.x);
        float currZ = (currDirection == InputUtil.INPUT_DIRECTION_UP) ? Mathf.Floor(transform.position.z) : Mathf.Ceil(transform.position.z);
        
        if (moving && !GamePaused
            && currX == newPlayerPosition.x
            && currZ == newPlayerPosition.z)
        {
            rb.linearVelocity = Vector3.zero;
            transform.position = newPlayerPosition;
            moving = false;

            playerSFXAudioSource.Stop();
            ResetAnimation();

            // If Goal is reached, stop the game and trigger success
            if (MazeManager.GetSpaceType(transform.position) == MazeManager.GOAL_SPACE)
            {
                GameOver = true;

                playerSFXAudioSource.Stop();
                playerSFXAudioSource.PlayOneShot(playerSFXs.Find(clip => clip.name == SFXClipMap.SFX_CLIP_NAME_LEVEL_COMPLETE));

                levelCompleteVFX.Play();
                playerMesh.SetActive(false);
                levelManager.StartCountdown(false);

                Invoke("BroadcastLevelComplete", 2);
            }
        }
    }

    private void BroadcastLevelComplete()
    {
        LevelCompleted?.Invoke();
    }

    private void ResetAnimation()
    {
        playerAnimator.SetBool(CubeAnimatorKeys.PARAM_MOVE_FORWARD, false);
        playerAnimator.SetBool(CubeAnimatorKeys.PARAM_MOVE_BACKWARD, false);
        playerAnimator.SetBool(CubeAnimatorKeys.PARAM_MOVE_RIGHT, false);
        playerAnimator.SetBool(CubeAnimatorKeys.PARAM_MOVE_LEFT, false);
    }

    // Computes new coordinates box would move to and starts move 
    private void AttemptMove(string direction)
    {
        newPlayerPosition = transform.position;

        switch (direction)
        {
            case InputUtil.INPUT_DIRECTION_UP:
                newPlayerPosition += Vector3.forward;
                break;
            case InputUtil.INPUT_DIRECTION_DOWN:
                newPlayerPosition += Vector3.back;
                break;
            case InputUtil.INPUT_DIRECTION_LEFT:
                newPlayerPosition += Vector3.left;
                break;
            case InputUtil.INPUT_DIRECTION_RIGHT:
                newPlayerPosition += Vector3.right;
                break;
            default:
                break;
        }

        if (IsSpaceAvailable(newPlayerPosition))
        {
            switch (direction)
            {
                case InputUtil.INPUT_DIRECTION_UP:
                    rb.AddForce(Vector3.forward * movementForce, ForceMode.Impulse);
                    playerAnimator.SetBool(CubeAnimatorKeys.PARAM_MOVE_FORWARD, true);
                    break;
                case InputUtil.INPUT_DIRECTION_DOWN:
                    rb.AddForce(Vector3.back * movementForce, ForceMode.Impulse);
                    playerAnimator.SetBool(CubeAnimatorKeys.PARAM_MOVE_BACKWARD, true);
                    break;
                case InputUtil.INPUT_DIRECTION_LEFT:
                    rb.AddForce(Vector3.left * movementForce, ForceMode.Impulse);
                    playerAnimator.SetBool(CubeAnimatorKeys.PARAM_MOVE_LEFT, true);
                    break;
                case InputUtil.INPUT_DIRECTION_RIGHT:
                    rb.AddForce(Vector3.right * movementForce, ForceMode.Impulse);
                    playerAnimator.SetBool(CubeAnimatorKeys.PARAM_MOVE_RIGHT, true);
                    break;
                default:
                    moving = false;
                    break;
            }

            if (moving)
            {
                playerSFXAudioSource.Stop();
                playerSFXAudioSource.PlayOneShot(playerSFXs.Find(clip => clip.name == SFXClipMap.SFX_CLIP_NAME_VALID_MOVE));
            }

            currDirection = direction;
        }
        else
        {
            playerSFXAudioSource.PlayOneShot(playerSFXs.Find(clip => clip.name == SFXClipMap.SFX_CLIP_NAME_INVALID_MOVE));
            moving = false;
        }
    }

    private bool IsSpaceAvailable(Vector3 coordinate)
    {
        string tileType = MazeManager.GetSpaceType(coordinate);
        return tileType != MazeManager.WALL_SPACE;
    }

    // Changes axis orientation of player and camera to face a new direction based on transition tile
    private void ChangeAxis(InputAction.CallbackContext context)
    {
        // If standing on top of transition tile
        // trigger camera movement animation based on transition tile metadata
        if(!GameOver && !GamePaused)
            if(MazeManager.GetSpaceType(transform.position) == MazeManager.TRANSITION_SPACE)
            {
                playerSFXAudioSource.Stop();
                playerSFXAudioSource.PlayOneShot(playerSFXs.Find(clip => clip.name == SFXClipMap.SFX_CLIP_NAME_MAZE_TRANSITION));

                GamePaused = true;
                mazeTransitionVFX.ForEach(vfx => vfx.Play());

                if (stopSmokeCR != null)
                    StopCoroutine(stopSmokeCR);

                StartCoroutine(StopTransitionSmoke());
            }
        return;
    }

    // Zooms out to the center of the maze to allow player have a bird's eye view on maze. Disables player movement
    private void ActivateBirdsEye(InputAction.CallbackContext context)
    {
        if(!GameOver && !GamePaused)
        {
            if (centerCameraCR != null)
                StopCoroutine(centerCameraCR);

            levelManager.ToggleActiveUI(false);
            BirdsEyeViewActive = true;
            playerSFXAudioSource.Stop();
            playerSFXAudioSource.PlayOneShot(playerSFXs.Find(clip => clip.name == SFXClipMap.SFX_CLIP_NAME_ZOOM_OUT));
            centerCameraCR = StartCoroutine(CenterCamera(new Vector3(MazeManager.mazeCenter[0], playerCamera.transform.position.y, MazeManager.mazeCenter[1]), MazeManager.mazeCenter[0], true));
        }
        return;
    }

    // Zooms in to the player and re-enables movement
    private void DeactivateBirdsEye(InputAction.CallbackContext context)
    {
        if(!GameOver && !GamePaused)
        {
            if (centerCameraCR != null)
                StopCoroutine(centerCameraCR);

            levelManager.ToggleActiveUI(true);
            playerSFXAudioSource.Stop();
            playerSFXAudioSource.PlayOneShot(playerSFXs.Find(clip => clip.name == SFXClipMap.SFX_CLIP_NAME_ZOOM_IN));
            centerCameraCR = StartCoroutine(CenterCamera(new Vector3(transform.position.x, playerCamera.transform.position.y, transform.position.z), CAMERA_ZOOM_IN_DEPTH, false, 0.6f));
        }
        return;
    }

    private IEnumerator CenterCamera(Vector3 endPos, float targetOrthographicSize, bool activateBirdsEye, float time = 1.0f)
    {
        while (time >= 0.0f)
        {
            Vector3 newPos = Vector3.Lerp(endPos, playerCamera.transform.position, time);
            playerCamera.transform.position = new Vector3(newPos.x, playerCamera.transform.position.y, newPos.z);

            playerCamera.GetComponent<Camera>().orthographicSize = Mathf.Lerp(targetOrthographicSize, playerCamera.GetComponent<Camera>().orthographicSize, time);

            time -= 0.05f;
            yield return new WaitForSeconds(0.05f);
        }
        BirdsEyeViewActive = activateBirdsEye;
        levelManager.StartCountdown(true);
    }

    private IEnumerator StopTransitionSmoke()
    {
        yield return new WaitForSeconds(0.8f);
        mazeTransitionVFX.ForEach(vfx => vfx.Stop());
        MazeManager.AttemptMazeTransition(transform.position);
        yield return new WaitForSeconds(1f);
        GamePaused = false;
    }
}
