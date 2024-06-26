
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[System.Serializable]
public class SkillBloodCost
{
    public string skillName;
    public float bloodCost;
}

public class PlayerController2 : MonoBehaviour
{
    public static PlayerController2 instance;

    // Movement
    [SerializeField]
    private float baseSpeed, pSpeed = 5f;
    [SerializeField]
    private float jumpForce = 15f;
    [SerializeField]
    private float wallSlidingSpeed = 2f;
    [SerializeField]
    private float VSpeedTick = 3;

    [SerializeField]
    private float maxDoorHeight;

    [SerializeField]
    private float minHeightBeforeDeath;

    [SerializeField]
    public float bloodAmount;

    private bool isButtonActive = false;
    public Rigidbody2D rBody;
    public CapsuleCollider2D pCollider;
    public SpriteRenderer pRenderer;
    [SerializeField]
    public Animator anim;

    // Wall Slide and Jump
    public Transform groundCheckPoint, wallCheckPoint;
    public LayerMask whatIsGround, whatIsWall;

    private bool isOnGround, isOnWall, isWallSliding, isWallJumping;

    private float wallJumpingDirection, wallJumpingCounter,
                    wallJumpingTime = 0.2f,
                    wallJumpingDuration = 0.4f;
    private Vector2 wallJumpingPower = new Vector2(8f, 16f);

    // Warp
    private GameObject curWarp;

    // Powers
    [SerializeField]
    private List<SkillBloodCost> skillBloodCostList = new List<SkillBloodCost>();

    public Dictionary<string, float> skillBloodCosts = new Dictionary<string, float>();
    public Dictionary<string, bool> activeSkills = new Dictionary<string, bool>();
    private Coroutine bloodCoroutine;

    public bool isVampVisEnabled = false,
        isVampSpdEnabled = false,
                hiddenLayerStatus = true;
    [SerializeField]
    private GameObject hLayer;

    private bool isAscending, isDecending;

    //Interaction
    public bool interactWithNote = false,
                interactWithNPC = false,
        interactWithPuzzle = false;
    GameObject currentNote, currentNPC;

    //Damage & Enemy
    [SerializeField]
    private float damageTaken;
    private bool collidedWithEnemy, damageCoroutineStarted;

    //Puzzle
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private GameObject puzzlePanel, puzzleDoor;
    [SerializeField]
    public CircleCollider2D envelopeTrigger;
    private bool puzzleOpened = false;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Cursor.visible = false;

        pRenderer = gameObject.GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        pCollider = GetComponent<CapsuleCollider2D>();
        hLayer = GameObject.FindGameObjectWithTag("Hidden");

        foreach (var skill in skillBloodCostList)
        {
            skillBloodCosts[skill.skillName] = skill.bloodCost;
            activeSkills[skill.skillName] = false; // Initialize all skills as inactive
        }
    }

    void Update()
    {
        if (Time.timeScale == 1)
        {
            Movement();
            Skills();

            isOnGround = Physics2D.OverlapCircle(groundCheckPoint.position, .05f, whatIsGround); //OverlapCircle tells if a circle in a position overlaps with another collider

            isOnWall = Physics2D.OverlapCircle(wallCheckPoint.position, .1f, whatIsWall);

            WallSlide();
            WallJump();

            UseWarp();
            checkDeath(); // makes all death checks (height and HP)


            if (interactWithNote)
            {

                InteractWithNote();
            }
            if (interactWithNPC)
            {
                InteractWithNPC();
            }
            if (interactWithPuzzle)
            {
                InteractWithPuzzle();
            }

            if ((collidedWithEnemy) && !damageCoroutineStarted)
            {
                StartCoroutine(Damage(damageTaken));
            }
            if (bloodAmount <= 0) { Reset(); }
        }

        if (puzzleOpened)
        {
            Puzzle();
        }
    }

    private void Movement()
    {
        rBody.velocity = new Vector2(pSpeed * Input.GetAxis("Horizontal"), rBody.velocity.y);
        anim.SetFloat("Speed", Mathf.Abs(rBody.velocity.x));
        anim.SetBool("WallSliding", isWallSliding);
        anim.SetBool("Grounded", isOnGround);
        anim.SetFloat("Yvelocity", rBody.velocity.y);

        //anim.SetBool("IsDecending", isDecending);

        if (Input.GetButtonDown("Jump") && isOnGround)
        {
            rBody.velocity = new Vector2(rBody.velocity.x, jumpForce);

        }

        if (Input.GetAxis("Horizontal") < 0)
        {
            pRenderer.flipX = true;
            wallCheckPoint.transform.position = new Vector2(gameObject.transform.position.x - 0.2f, transform.position.y);
            wallCheckPoint.transform.rotation = new Quaternion(0, 180, 0, 0);
        }
        else if (Input.GetAxis("Horizontal") > 0)
        {
            pRenderer.flipX = false;
            wallCheckPoint.transform.position = new Vector2(gameObject.transform.position.x + 0.2f, transform.position.y);
            wallCheckPoint.transform.rotation = new Quaternion(0, 0, 0, 0);
        }

        if (rBody.velocity.y > 1)
        {
            isAscending = true;
            isDecending = false;
        }
        else if (rBody.velocity.y < -1)
        {
            isAscending = false;
            isDecending = true;
        }
        else
        {
            isAscending = false;
            isDecending = false;
        }
    }

    private void Skills()
    {
        if (Input.GetButtonDown("Speed"))
        {
            ToggleSkill("Speed");
        }

        if (Input.GetButtonDown("Vision"))
        {
            ToggleSkill("Vision");
        }

        if (activeSkills.ContainsValue(true))
        {
            if (bloodCoroutine == null)
            {
                bloodCoroutine = StartCoroutine(SpendBloodOverTime());
            }

        }

        else
        {
            if (bloodCoroutine != null)
            {
                StopCoroutine(bloodCoroutine);
                bloodCoroutine = null;
            }
        }
        if (activeSkills["Speed"])
        {
            VampSpeed(true);
            isVampSpdEnabled = true;
        }
        else
        {
            VampSpeed(false);
            isVampSpdEnabled = false;
        }

        if (activeSkills["Vision"])
        {
            toggleHidden(false);
        }
        else
        {
            toggleHidden(true);

        }
    }

    private void ToggleSkill(string skillName)
    {
        activeSkills[skillName] = !activeSkills[skillName];
    }

    private IEnumerator SpendBloodOverTime()
    {
        while (true)
        {
            float totalBloodCost = 0f;

            foreach (var skill in activeSkills)
            {
                if (skill.Value)
                {
                    totalBloodCost += skillBloodCosts[skill.Key];
                }
            }

            if (bloodAmount >= totalBloodCost)
            {
                bloodAmount -= totalBloodCost;
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                StopAllSkills();
                break;
            }
        }
    }

    private void StopAllSkills()
    {
        foreach (var skill in activeSkills.Keys.ToList())
        {
            activeSkills[skill] = false;
        }
        if (bloodCoroutine != null)
        {
            StopCoroutine(bloodCoroutine);
            bloodCoroutine = null;
        }
    }

    private void Reset()
    {
        GameObject.Find("DeathScreen").GetComponent<Image>().enabled = true;
        GameObject.Find("Text (TMP) - Death").GetComponent<TextMeshProUGUI>().enabled = true;
        if (Input.anyKeyDown)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void changeScene(string scene)
    {
        SceneManager.LoadScene(scene);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject obj = GameObject.FindGameObjectWithTag("Object");
        if (obj != null)
        {

            Rigidbody2D rBodyObj = obj.GetComponent<Rigidbody2D>();
            if (collision.gameObject.CompareTag("Object"))
            {
                rBodyObj.isKinematic = true;
            }
        }


        if (collision.gameObject.CompareTag("Enemy"))
        {
            collidedWithEnemy = true;
            anim.ResetTrigger("HurtEnded");
            anim.SetTrigger("Hurt");
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Object"))
        {

            Rigidbody2D rBodyObj = collision.gameObject.GetComponent<Rigidbody2D>();
            rBodyObj.isKinematic = false;
        }
        if (collision.gameObject.CompareTag("Enemy"))
        {
            collidedWithEnemy = false;
            anim.ResetTrigger("Hurt");
            anim.SetTrigger("HurtEnded");

        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Button"))
        {
            isButtonActive = true;
        }
        else if (collision.gameObject.CompareTag("Warp"))
        {
            curWarp = collision.gameObject;
        }
        else if (collision.gameObject.CompareTag("Hidden"))
        {
            toggleHidden(false);
        }

        if (collision.gameObject.CompareTag("Scene"))
        {
            changeScene(collision.gameObject.name);
        }
        if (collision.gameObject.CompareTag("Puzzle"))
        {
            interactWithPuzzle = true;
        }
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Note"))
        {
            currentNote = collision.gameObject;
            interactWithNote = true;

        }
        else if (collision.gameObject.CompareTag("NPC"))
        {
            currentNPC = collision.gameObject;
            interactWithNPC = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Button"))
        {
            isButtonActive = false;
        }
        if (collision.CompareTag("Warp"))
        {
            if (collision.gameObject == curWarp)
            {
                curWarp = null;
            }
        }
        if (collision.gameObject.CompareTag("Hidden"))
        {
            toggleHidden(true);
        }
        if (collision.gameObject.CompareTag("Note"))
        {
            currentNote = null;
            interactWithNote = false;

        }
        if (collision.gameObject.CompareTag("NPC"))
        {
            currentNPC = null;
            interactWithNPC = false;
        }
    }

    IEnumerator Damage(float damage)
    {
        damageCoroutineStarted = true;
        while (collidedWithEnemy)
        {
            bloodAmount -= damage;
            yield return new WaitForSeconds(1);
        }
        damageCoroutineStarted = false;
    }

    private void UseWarp()
    {
        if (curWarp != null)
        {
            if (Input.GetButtonDown("Interact"))
            {
                transform.position = curWarp.GetComponent<getDest>().getD().position;
            }
        }
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localRotation.x;
            wallJumpingCounter = wallJumpingTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }
        if (Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rBody.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            if (transform.localScale.x != wallJumpingDirection)
            {
                pRenderer.flipX = !pRenderer.flipX;
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    private void WallSlide()
    {
        if (isOnWall && !isOnGround)
        {
            isWallSliding = true;
            rBody.velocity = new Vector2(rBody.velocity.x, 0);
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void checkDeath()
    {
        if (bloodAmount <= 0)
        {
            // Reset();
        }
        if (gameObject.transform.position.y < minHeightBeforeDeath)
            Reset();
    }

    private void VampSpeed(bool vSpeedEnabled)
    {
        if (vSpeedEnabled && skillBloodCosts["Speed"] <= bloodAmount)
        {
            pSpeed = 10f;

        }
        else
        {
            pSpeed = baseSpeed;
        }
    }

    private void VampVision(bool vVisionEnabled)
    {
        if (vVisionEnabled && skillBloodCosts["Vision"] <= bloodAmount)
        {
            isVampVisEnabled = true;
        }
        else
        {
            isVampVisEnabled = false;
        }
    }

    private void InteractWithNote()
    {
        if (Input.GetButtonDown("Interact") && interactWithNote)
        {
            if (!NotesController.instance.isNoteOpen)
            {
                NotesController.instance.OpenNote(currentNote.name);
            }
        }

    }
    private void InteractWithPuzzle()
    {
        if (Input.GetButtonDown("Interact") && interactWithPuzzle)
        {
            if (puzzlePanel != null)
            {
                puzzlePanel.SetActive(true);
                inputField.Select();
                inputField.ActivateInputField();
                Time.timeScale = 0;
                puzzleOpened = true;
            }
        }





    }
    private void InteractWithNPC()
    {
        if (Input.GetButtonDown("Interact") && interactWithNPC)
        {
            if (!Dialogue.instance.isDialogueOpen)
            {
                Dialogue.instance.StartDialogue(currentNPC.name);

            }
        }
    }

    private void Puzzle()
    {
        if (Input.GetButton("Submit") && inputField.text.ToLower() == "lucem sequimur")
        {
            puzzlePanel.SetActive(false);
            puzzleDoor.SetActive(false);
            Time.timeScale = 1;
            envelopeTrigger.enabled = false;
        }
        else if (Input.GetButton("Submit") && inputField.text.ToLower() != "lucem sequimur")
        {
            inputField.text = string.Empty;
            inputField.Select();
            inputField.ActivateInputField();
        }
        else if (Input.GetButton("Cancel"))
        {
            Time.timeScale = 1;
            puzzlePanel.SetActive(false);

        }
    }

    private void toggleHidden(bool hidden)
    {
        if (hLayer != null)
        {
            if (!hidden) //hidden layer is active and there is a request to disable it
            {
                hLayer.transform.GetComponent<TilemapRenderer>().enabled = false;
                hiddenLayerStatus = false;
                return;
            }

            if (hidden) //hidden layer is closed and there is a request to enable it
            {
                hLayer.transform.GetComponent<TilemapRenderer>().enabled = true;
                hiddenLayerStatus = true;
                return;
            }
        }

    }
}
