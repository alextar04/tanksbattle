﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using TMPro;

public class EnemyTanks : MonoBehaviour
{
	public float speed = 1f;
    public float rotationSpeed = 90f;
    // Кулдаун пушки вражеского танка =  3с.
    public float cooldown = 3f;
    public float timer = 0f;
    // Префабы(ссылки на них) задаются в редакторе
    public GameObject enemyTankPrefab;
    public GameObject bullet;
    public GameObject dulo;
    public float speedBullet = 5f;
    public NavMeshAgent aiEnemyTank;

    // Здоровье врага
    public float healthPointTotal = 500;
    public float healthPointCurrent = 500;
    public float countDamage = 100;
    public Vector3 pivot = Vector3.up;
    public bool alive = true;

    // Сгенерированные вражеские танки
    public List<GameObject> listEnemyTanks;
    public int totalGenerated = 1;
    public int totalMaxGenerated = 10;

    // Цель вражеских танков
    private GameObject RedTank;
    private RedTank RedTankManipulator;
    private GameObject GreenTank;
    private GreenTank GreenTankManipulator;

    
    // Отрисовка сгенерированного бонуса
    void Start()
    {
    	if (enemyTankPrefab.gameObject.name == "EnemyTankBlack"){
    		listEnemyTanks.Add(enemyTankPrefab);
        	InvokeRepeating("NewEnemyTankGenerate", 5, 10);
    	}
    	RedTank = GameObject.Find("RedTankMaus");
    	RedTankManipulator = RedTank.GetComponent<RedTank>();
    	GreenTank = GameObject.Find("GreenTank");
    	GreenTankManipulator = GreenTank.GetComponent<GreenTank>();
    }

    // Проверка на получение урона
    void OnCollisionEnter(Collision myTrigger){
  		if (myTrigger.gameObject.name == "firstLevelBullet(Clone)")
  		{
    		healthPointCurrent -= 250;
    		Debug.Log("Damaged: " + enemyTankPrefab.gameObject.name + " " + healthPointCurrent);
    		Destroy(myTrigger.gameObject);
  		}

  		if (myTrigger.gameObject.name == "fiveLevelBullet(Clone)"){
  			Destroy(myTrigger.gameObject);
  		}

  		// Бонус-аптечка
        if (myTrigger.gameObject.name == "firstaid(Clone)")
        {
            if (healthPointCurrent + 100 <= healthPointTotal)
                healthPointCurrent += 100;
            else
                healthPointCurrent = healthPointTotal;
            Destroy(myTrigger.gameObject);
            Debug.Log("HealthBox removed: ");
        }
	}


	// Генерирование стартовой позиции вражеского танка
    Vector3 RandomPosition()
    {
        Vector3 generatedPosition = new Vector3(2.611f, 7.05f, 1.42f);
        return generatedPosition;
    }

    void NewEnemyTankGenerate()
    {
        // Дабы не создавать потомков от клонов
        // Создаем итого 10 новых танков
        if ((enemyTankPrefab.gameObject.name == "EnemyTankBlack") && (totalGenerated < totalMaxGenerated)) {
            GameObject instantedObject;
            instantedObject = Instantiate(enemyTankPrefab, RandomPosition(), Quaternion.identity) as GameObject;
            int index = listEnemyTanks.Count + 1;
            instantedObject.name = "EnemyTankBlack " + index.ToString();
            instantedObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            instantedObject.transform.eulerAngles = new Vector3(0.0f, 180.0f, 0.0f);
            instantedObject.SetActive(true);
            // Зададим свойства
            EnemyTanks enemy = instantedObject.GetComponent<EnemyTanks>();
            enemy.timer = cooldown;
            enemy.speed = speed;
            enemy.healthPointCurrent = healthPointTotal;
            enemy.pivot = Vector3.up;
            enemy.alive = true;
            enemy.aiEnemyTank.enabled = false;

            // Новый сгенерированный танк хранится в списке (как наследованные от главного)
            EnemyTanks mainEnemy = enemyTankPrefab.GetComponent<EnemyTanks>();
            mainEnemy.listEnemyTanks.Add(instantedObject);
            totalGenerated += 1;

            // Cписок управляемых танков дочерними танками пуст
            List<GameObject> listEnemyTanksClone = new List<GameObject> (listEnemyTanks);
            listEnemyTanksClone.Clear();
            enemy.listEnemyTanks = listEnemyTanksClone;
        }
    }

    // Произведение выстрела
    void fire()
    {
        // Раз в 10 секунд -> произвести выстрел
        if ((timer == 0 || ((cooldown - timer/2) < 0.05)) && alive)
        {
            // Координаты дула
            Vector3 SpawnPoint = dulo.transform.position;
            Quaternion SpawnRoot = dulo.transform.rotation;
            // Создание пули
            GameObject bulletForFire = Instantiate(bullet, SpawnPoint, SpawnRoot) as GameObject;
            // Придание ей ускорения (Rigidbody берется у bullet)
            Rigidbody Run = bulletForFire.GetComponent<Rigidbody>();
            Run.AddForce(bulletForFire.transform.right * speedBullet, ForceMode.Impulse);
            Destroy(bulletForFire, 5);
            // Выставить кулдаун
            timer = cooldown;
        }
    }


    public float xLeft = -6f;
    public float xRight = 3f;
    public float zTop = -4.5f;
    public float zBot = 4.5f;
    public Vector3 generatedPosition;

    void Update()
    {

        if (timer > 0)
            timer -= Time.deltaTime;
        else
            timer = 0;

        // Слушатель прослушивает прошедшее время и стреляет по таймеру
        fire();
        int indexRemove = -1;
        foreach (GameObject tank in listEnemyTanks){
        	// Вызвать деструктор мертвого танка
        	EnemyTanks enemy = tank.GetComponent<EnemyTanks>();
        	 if (enemy.healthPointCurrent <= 0){
                indexRemove = listEnemyTanks.IndexOf(tank);
                enemy.alive = false;
                enemy.aiEnemyTank.enabled = false;
        	 	continue;
        	 }

        	 // Позиция танка по y не должна превышать 7.15, иначе он не двигается
        	 // Например, лежит на боку
        	 float yPosition = enemy.transform.position.y;
        	 if (yPosition < 7.20f){
        	 	// Передвижение ботов
        	 	float distanceRed = Vector3.Distance(RedTankManipulator.transform.position, enemy.transform.position);
        	 	float distanceGreen = Vector3.Distance(GreenTankManipulator.transform.position, enemy.transform.position);
        	 	Debug.Log("Дистанция: " + distanceRed);
        	 	if (enemy.alive){
        	 		// Враг поворачивает на красный танк
        	 		enemy.aiEnemyTank.enabled = true;
        	 		Debug.Log("Доступность ИИ:" + enemy.aiEnemyTank.enabled);
        	 		
			        Vector3 rectangleToRun;
			        if (RedTankManipulator.alive == true && GreenTankManipulator.alive == true){
			        	// Динамический выбор цели (выбираем ближайшую)
				        if (distanceRed < distanceGreen){
				        	float centerRectangleX = RedTankManipulator.transform.position.x;
				        	float centerRectangleY = RedTankManipulator.transform.position.y;
				        	float centerRectangleZ = RedTankManipulator.transform.position.z;
				        	float centerRectangleXLeft = centerRectangleX - 4;
				        	float centerRectangleXRight = centerRectangleX + 4;
				        	float centerRectangleZLow = centerRectangleZ - 4;
				        	float centerRectangleZHigh = centerRectangleZ + 4;
				        	Vector3 RandomPosition = new Vector3(UnityEngine.Random.Range(centerRectangleXLeft, centerRectangleXRight), 
				        										7.05f,
				        		 								UnityEngine.Random.Range(centerRectangleZLow, centerRectangleZHigh));
				            // rectangleToRun = RedTankManipulator.transform.position;
				            Debug.Log("Сгенерированная позиция красный: " + RandomPosition);
				            rectangleToRun = RandomPosition;
				   		 }
				        else{
				            float centerRectangleX = GreenTankManipulator.transform.position.x;
				        	float centerRectangleY = GreenTankManipulator.transform.position.y;
				        	float centerRectangleZ = GreenTankManipulator.transform.position.z;
				        	float centerRectangleXLeft = centerRectangleX - 4;
				        	float centerRectangleXRight = centerRectangleX + 4;
				        	float centerRectangleZLow = centerRectangleZ - 4;
				        	float centerRectangleZHigh = centerRectangleZ + 4;
				        	Vector3 RandomPosition = new Vector3(UnityEngine.Random.Range(centerRectangleXLeft, centerRectangleXRight), 
				        										7.05f,
				        		 								UnityEngine.Random.Range(centerRectangleZLow, centerRectangleZHigh));
				            // rectangleToRun = RedTankManipulator.transform.position;
				            Debug.Log("Сгенерированная позиция зеленый: " + RandomPosition);
				            rectangleToRun = RandomPosition;
				        	}
				        }
			        	else{
			        		if (RedTankManipulator.alive == true)
			        			rectangleToRun = RedTankManipulator.transform.position;
			        				else
			        					rectangleToRun = GreenTankManipulator.transform.position;
			        		}
        	 		//rectangleToRun.x /= 2;
        	 		//rectangleToRun.z /= 2;
        	 		enemy.aiEnemyTank.SetDestination(rectangleToRun);
        	 	}
        	 	else
        	 		enemy.aiEnemyTank.enabled = false;
        	}
    	}

    	GameObject mainEnemy = GameObject.Find("EnemyTankBlack");
        EnemyTanks mainEnemyManipulator = mainEnemy.GetComponent<EnemyTanks>();
        // Если есть, что удалять, то удалить из потомков главного танка
        if (indexRemove != -1){
            // Удаление танка из массива потомков главного танка
            Debug.Log("Removing " + (indexRemove+1) + " танк");
            mainEnemyManipulator.listEnemyTanks.RemoveAt(indexRemove);
        }
        // Если нечего удалять - поражение
        if (((mainEnemyManipulator.listEnemyTanks.Count == 0) && (mainEnemyManipulator.totalGenerated == mainEnemyManipulator.totalMaxGenerated)) ||
        		((RedTankManipulator.alive == false) && (GreenTankManipulator.alive == false)))
        	OpenFinishMenu();
	}


	// Часть, связанная с окончанием игры
	public GameObject finishObject;
    public TextMeshProUGUI manipulatorText;

    public void OpenFinishMenu()
    {
    	finishObject.gameObject.SetActive(true);
        // Найти объект по имени
		// Выясним выиграли мы или проиграли
		GameObject mainEnemy = GameObject.Find("EnemyTankBlack");
        EnemyTanks mainEnemyManipulator = mainEnemy.GetComponent<EnemyTanks>();

		// взять переменную здоровья
		int totalAlive = mainEnemyManipulator.listEnemyTanks.Count;
        if (totalAlive != 0){
        	manipulatorText.text = "Победа ботов в сражении!";
        }else{
        	manipulatorText.text = "Победа союзников в сражении!";
    	}
	}

    public void OpenStartMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }



}
