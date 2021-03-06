using POLIMIGameCollective;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameplayManager : Singleton<GameplayManager> {
    
	[Header("Sounds and Audio Effects")]
    // Sound manager.
	public MusicManager musicManager;

    // confirmed selection from Character Selection Menu
    public static Character chosenCharacter;

	[Header("Characters Stats")]
    // Characters
    public Character AirPlayer;
	public Character FirePlayer;
	public Character EarthPlayer;
	public Character WaterPlayer;

    [Header("UI Screens")]
    public GameObject inGameMenuScreen;
    public GameObject healthScreen;
    public Image healthBar;
    public Text healthText;
    public Text keyAirText;
    public Text keyEarthText;
    public Text keyFireText;
    public Text keyWaterText;
    public Text coinText;
    public GameObject overlayScreen;
    public GameObject victoryMessage;
    public GameObject defeatMessage;
    public GameObject effectDamageObject;

    [Header("Player")]
    public Player m_player;
    private CharacterManager playerChar;

    [Header("Prefabs")]
    public GameObject m_SlashAttack;
    public GameObject m_ThrustAttack;
	public GameObject m_AreaAttack;
    public GameObject m_RangedAttack;
    public GameObject m_drop;

    private GameObject[] secondaryDropKey = new GameObject[System.Enum.GetValues(typeof(ElementType)).Length];
    private GameObject[] secondaryDropCoin = new GameObject[System.Enum.GetValues(typeof(ElementType)).Length];
    private GameObject[] secondaryDropHeart = new GameObject[System.Enum.GetValues(typeof(ElementType)).Length];
    private GameObject[] secondaryDropGems = new GameObject[System.Enum.GetValues(typeof(ElementType)).Length];

    // We create a dictionary where the keys will be the instance ID of the attacks (they're managed by the pooling manager)
    // and the values will be the CharacterManager of the attacker using that instance, this, in order to have the 
    // stats of the attacker.
    public Dictionary<int,CharacterManager> attackersDict = new Dictionary<int,CharacterManager> ();

    // Number of killed bosses, by element.
    private int[] noKilledBosses = new int[System.Enum.GetValues(typeof(ElementType)).Length];

	private bool isGameOver = false;

    // Use this for initialization
    void Start ()
    {
        ObjectPoolingManager.Instance.CreatePool(m_SlashAttack, 30, 30);
        ObjectPoolingManager.Instance.CreatePool(m_ThrustAttack, 30, 30);
        ObjectPoolingManager.Instance.CreatePool (m_AreaAttack, 30, 30);
        ObjectPoolingManager.Instance.CreatePool(m_RangedAttack, 100, 100);
        ObjectPoolingManager.Instance.CreatePool (m_drop, 100, 100);

		inGameMenuScreen.SetActive(false);
        healthScreen.SetActive(true);
        overlayScreen.SetActive(false);

        playerChar = m_player.GetComponent<CharacterManager>();
        playerChar.InitCharacter(chosenCharacter);
        UpdateHealthBar();
        UpdateCoinBar();
        UpdateKeyBar();

		isGameOver = false;
    }
	
	// Update is called once per frame
	void Update () {

        if (Input.GetKeyDown(KeyCode.O))
        {
			inGameMenuScreen.SetActive(!inGameMenuScreen.activeInHierarchy);
            healthScreen.SetActive(!healthScreen.activeInHierarchy);
        }
        //		if (Input.GetKeyDown (KeyCode.Alpha1))
        //			SfxManager.Instance.Play ("creature");
        //		else if (Input.GetKeyDown (KeyCode.Alpha2))
        //			SfxManager.Instance.Play ("jump");
        //		else if (Input.GetKeyDown (KeyCode.Alpha3))
        //			SfxManager.Instance.Play ("laser");
        //		else if (Input.GetKeyDown (KeyCode.Alpha4))
        //			SfxManager.Instance.Play ("lose");
        //		else if (Input.GetKeyDown (KeyCode.Alpha5))
        //			SfxManager.Instance.Play ("pickup");
        //		else if (Input.GetKeyDown (KeyCode.Alpha6))
        //			SfxManager.Instance.Play ("radar");
        //		else if (Input.GetKeyDown (KeyCode.Alpha7))
        //			SfxManager.Instance.Play ("rumble");
        //		else if (Input.GetKeyDown (KeyCode.Space)) {
        //			MusicManager.Instance.StopAll ();
        //			MusicManager.Instance.PlayMusic ("MenuMusic");
        //			SceneManager.LoadScene ("Menu");
        //		}
    }

    #region Attack Management
    public void ExecuteAttack(CharacterManager attacker, CharacterManager defender)
    {
        if (!defender.isDead())
        {
            double damage = GameLogicManager.CalculateDamage(attacker, defender);
            defender.ApplyDamage(damage);

            // check abilities that trigger on attack
            AbilityManager.CheckTriggeredAbilitiesActivation(TriggeredTriggerType.OnInflictedAttack, attacker, defender);
            AbilityManager.CheckTriggeredAbilitiesActivation(TriggeredTriggerType.OnReceivedAttack, defender, attacker);

            if (defender.isDead())
            {
                // check abilities that trigger on death
                AbilityManager.CheckTriggeredAbilitiesActivation(TriggeredTriggerType.OnKill, attacker, defender);
                AbilityManager.CheckTriggeredAbilitiesActivation(TriggeredTriggerType.OnDeath, defender, attacker);

                // check again in case of resurrection
                if (defender.isDead())
                {
                    Kill(defender);
                }
            }

            if (defender.gameObject.CompareTag("Player"))
            {
                UpdateHealthBar();
            }
        }
    }

    public void Kill(CharacterManager deadCharacter)
    {
        if (deadCharacter.gameObject.CompareTag("Player"))
        {
			if (!isGameOver) { 
				isGameOver = true;
				StartCoroutine(GameOver());
			}
        }
        else if (deadCharacter.gameObject.CompareTag("Boss"))
        {
			StartCoroutine(SpawnDrops(deadCharacter));
            StartCoroutine(SpawnGem(deadCharacter));
            deadCharacter.gameObject.SetActive(false);

            noKilledBosses[(int)deadCharacter.Element]++;
            bool victory = true;
            for (int i = 0; victory && i < System.Enum.GetValues(typeof(ElementType)).Length; i++)
            {
                if (noKilledBosses[i] <= 0)
                {
                    victory = false;
                }
            }
            if (victory)
            {
                StartCoroutine(Victory());
            }
        }
		//else if (deadCharacter.gameObject.CompareTag("Enemy"))
		else {
			StartCoroutine(SpawnDrops(deadCharacter));
			deadCharacter.gameObject.SetActive (false);
		}

    }

    public void UpdateHealthBar()
    {
        double totalVitality = System.Math.Round(playerChar.Stats[(int)StatType.VIT].FinalStat, 1);
        double currentVitality = System.Math.Round(playerChar.Stats[(int)StatType.VIT].FinalStat - playerChar.Damage, 1);

        healthBar.GetComponent<RectTransform>().localScale = new Vector2((float)(currentVitality / totalVitality), 1);
        healthText.text = currentVitality + " / " + totalVitality;
    }

    public void UpdateKeyBar()
    {
        keyFireText.text = "    " + playerChar.Keys[(int)ElementType.Fire];
        keyEarthText.text = "    " + playerChar.Keys[(int)ElementType.Earth];
        keyWaterText.text = "    " + playerChar.Keys[(int)ElementType.Water];
        keyAirText.text = "    " + playerChar.Keys[(int)ElementType.Air];
    }

    public void UpdateCoinBar()
    {
        coinText.text = "      " + playerChar.Money;
    }

    public void showDamage(double damage, Vector3 position)
    {
        GameObject go;
        Text tx;
        float x = Camera.main.transform.position.x - position.x;
        float y = Camera.main.transform.position.y - position.y;
        go = Instantiate(effectDamageObject) as GameObject;
        go.GetComponent<Canvas>().transform.Translate(position);
        go.SetActive(true);
        tx = go.transform.Find("Text").GetComponent<Text>();
        if (damage > 0)
        {
            tx.text = "- " + damage;
            tx.color = Color.white;
        }
        else if (damage < 0)
        {
            tx.text = "+ " + -damage;
            tx.color = Color.green;
        }
        StartCoroutine(hideDamage(go));
    }

    private IEnumerator hideDamage(GameObject go)
    {
        yield return new WaitForSeconds(1);
        Destroy(go);
    }

    #endregion

    #region Drops Management

    public IEnumerator SpawnGem(CharacterManager deadCharacter)
    {
        GameObject go;
        go = Instantiate(secondaryDropGems[(int)deadCharacter.Element], deadCharacter.gameObject.transform.position, deadCharacter.gameObject.transform.rotation, deadCharacter.gameObject.transform.parent) as GameObject;

        go.GetComponent<usableObject>().direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
        go.GetComponent<usableObject>().shouldMove = true;

        yield return new WaitForSeconds(1);

        go.GetComponent<usableObject>().shouldMove = false;
        go.GetComponent<CircleCollider2D>().isTrigger = true;
    }

    public IEnumerator SpawnDrops(CharacterManager character)
    {
        if (character.Inventory != null)
        {
			GameObject go;
            List<Drop> drops = new List<Drop>();
            double luck = m_player.GetComponent<CharacterManager>().Stats[(int)StatType.LCK].FinalStat;

            foreach (Item i in character.Inventory)
            {
                if (i != null && (Random.Range(0f, 100f) <= i.dropRate * (1 + luck / 10)))
                {

                    //spawn the object
                    go = ObjectPoolingManager.Instance.GetObject(m_drop.name);
                    go.transform.position = character.transform.position;
                    go.transform.rotation = Quaternion.identity;
                    go.GetComponent<SpriteRenderer>().sprite = i.sprite;
                    go.SetActive(true);

                    //define item
                    Drop drop = go.GetComponent<Drop>() as Drop;
                    drop.item = i;
                    drops.Add(drop);

                    //give a random direction to the explosion
                    drop.direction = new Vector3(
                        UnityEngine.Random.Range(-1f, 1f),
                        UnityEngine.Random.Range(-1f, 1f),
                        0f
                    );

                    //enable movement
                    drop.shouldMove = true;
                }
            }

	        List<GameObject> secondaryDrops = new List<GameObject>();
	        if (Random.Range(0, 7) == 0)
	        {
	            go = Instantiate(secondaryDropKey[(int)character.Element], character.transform.position, Quaternion.identity, character.transform.parent) as GameObject;
	            secondaryDrops.Add(go);
	            go.GetComponent<usableObject>().direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
	            go.GetComponent<usableObject>().shouldMove = true;
	        }
	        if (Random.Range(0, 10) == 0)
	        {
	            go = Instantiate(secondaryDropHeart[(int)character.Element], character.transform.position, Quaternion.identity, character.transform.parent) as GameObject;
	            secondaryDrops.Add(go);
	            go.GetComponent<usableObject>().direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
	            go.GetComponent<usableObject>().shouldMove = true;
	        }
	        if (Random.Range(0, 5) == 0)
	        {
	            go = Instantiate(secondaryDropCoin[(int)character.Element], character.transform.position, Quaternion.identity, character.transform.parent) as GameObject;
	            secondaryDrops.Add(go);
	            go.GetComponent<usableObject>().direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f);
	            go.GetComponent<usableObject>().shouldMove = true;
	        }

	        yield return new WaitForSeconds(1);

	        //disable movement
	        foreach (Drop drop in drops)
	        {
	            drop.shouldMove = false;
            }

	        foreach (GameObject drop in secondaryDrops)
	        {
	            drop.GetComponent<usableObject>().shouldMove = false;
	            drop.GetComponent<CircleCollider2D>().isTrigger = true;
	            drop.layer = 0;
	        }
    	}
	}

    public void openChest(GameObject chest)
    {
        StartCoroutine(SpawnChestDrops(chest));
    }

    public IEnumerator SpawnChestDrops(GameObject chest)
    {
        List<GameObject> objects = new List<GameObject>();
        List<Drop> drops = new List<Drop>();
        Item i = chest.GetComponent<chestEnemiesActivator>().item;
        if (i != null)
        {
            //spawn the object
            GameObject go = ObjectPoolingManager.Instance.GetObject(m_drop.name);
            go.transform.position = chest.transform.position;
            go.transform.rotation = Quaternion.identity;
            go.GetComponent<SpriteRenderer>().sprite = i.sprite;
            go.SetActive(true);

            //define item
            Drop drop = go.GetComponent<Drop>() as Drop;
            drop.item = i;
            drops.Add(drop);

            //give a random direction to the explosion
            drop.direction = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f
            );

            //enable movement
            drop.shouldMove = true;
        }

        foreach (GameObject drop in chest.GetComponent<chestEnemiesActivator>().objects)
        {
            objects.Add(drop);
            drop.GetComponent<usableObject>().direction = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-1f, 1f),
                0f
            );
            drop.SetActive(true);
            drop.transform.parent = drop.transform.parent.parent;
            drop.GetComponent<usableObject>().shouldMove = true;
        }

        Destroy(chest);

        yield return new WaitForSeconds(1);

        //disable movement
        foreach (Drop drop in drops)
        {
            drop.shouldMove = false;
        }

        foreach (GameObject drop in objects)
        {
            drop.GetComponent<usableObject>().shouldMove = false;
            drop.GetComponent<CircleCollider2D>().isTrigger = true;
            drop.layer = 0;
        }
        //character.gameObject.SetActive (false);

    }

    public void setSecondaryDropKey(GameObject[] secondary)
    {
        secondaryDropKey = secondary;
    }

    public void setSecondaryDropCoin(GameObject[] secondary)
    {
        secondaryDropCoin = secondary;
    }

    public void setSecondaryDropHeart(GameObject[] secondary)
    {
        secondaryDropHeart = secondary;
    }
    
    public void setSecondaryDropGems(GameObject[] secondary)
    {
        secondaryDropGems = secondary;
    }

    public void PickUpDrop(Drop drop)
    {
        if(m_player.GetComponent<CharacterManager>().AddItem(drop.item))
        {
            drop.gameObject.SetActive(false);
        }
    }
    #endregion

    #region Game Ending Management
    IEnumerator GameOver()
    {
		m_player.isDead = true;

		yield return new WaitForSeconds(1f);

        inGameMenuScreen.SetActive(false);
        healthScreen.SetActive(true);
        overlayScreen.SetActive(true);
        defeatMessage.SetActive(true);

        yield return new WaitForSeconds(3f);

        inGameMenuScreen.SetActive(false);
        healthScreen.SetActive(false);
        overlayScreen.SetActive(false);
        defeatMessage.SetActive(false);

		// Changing the background music to main menu music.
		GameplayManager.Instance.StopAllMusic ();
		GameplayManager.Instance.PlayMusic(Constants.MUSIC_Menu);

        SceneManager.LoadScene("Main Menu");
    }

    IEnumerator Victory()
    {
        yield return new WaitForSeconds(1f);

        inGameMenuScreen.SetActive(false);
        healthScreen.SetActive(true);
        overlayScreen.SetActive(true);
        victoryMessage.SetActive(true);

        yield return new WaitForSeconds(3f);

        inGameMenuScreen.SetActive(false);
        healthScreen.SetActive(false);
        overlayScreen.SetActive(false);
        victoryMessage.SetActive(false);

		// Changing the background music to main menu music.
		GameplayManager.Instance.StopAllMusic ();
		GameplayManager.Instance.PlayMusic(Constants.MUSIC_Menu);

        SceneManager.LoadScene("Main Menu");
    }
    #endregion

	#region Game Music Management
	public void PlayMusic(string musicName, float pitchVariance = 0) {
		MusicManager.Instance.PlayMusic (musicName, pitchVariance);
	}

	public void PlayMusicWithBackground(string musicName, float pitchVariance = 0) {
		MusicManager.Instance.PlayMusic (Constants.MUSIC_Background);
		MusicManager.Instance.PlayMusic (musicName, pitchVariance);
	}

	public void StopMusic(string musicName, float pitchVariance = 0) {
		MusicManager.Instance.StopMusic (musicName, pitchVariance);
	}

	public void StopAllMusic() {
		MusicManager.Instance.StopAll ();
	}
    #endregion

    public int getNoKilledBosses (int element)
    {
        return noKilledBosses[element];
    }
}
