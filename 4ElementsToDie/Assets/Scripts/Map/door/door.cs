﻿using UnityEngine;
using System.Collections;

public class door : MonoBehaviour
{
    protected bool inDoorArea;
    protected GameObject player;
    public GameObject buttom;
    public GameObject where;

    void Start()
    {
        inDoorArea = false;
    }

    void Update()
    {
        if (inDoorArea && Input.GetKeyDown(KeyCode.F))
        {
            Vector3 mouvement = new Vector3(0, 3.5f, 0);
            player.transform.position = transform.rotation * mouvement + transform.position;
            mouvement = new Vector3(0, 6, 0);
            mouvement = transform.rotation * mouvement + transform.position;
            Camera.main.transform.position = new Vector3(mouvement.x, mouvement.y, Camera.main.transform.position.z);
            Camera.main.backgroundColor = new Color(0, 0, 0);

            // Changing the sound.
            GameplayManager.Instance.StopAllMusic();
			GameplayManager.Instance.PlayMusic(Constants.MUSIC_Background);


            inDoorArea = false;
            where.SetActive(true);
            transform.parent.gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            buttom.SetActive(true);
            player = other.gameObject;
            inDoorArea = true;
        }
        else return;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            buttom.SetActive(false);
            inDoorArea = false;
        }
        else return;
    }
}
