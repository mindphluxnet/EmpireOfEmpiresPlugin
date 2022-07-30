using BepInEx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace EmpireOfEmpiresPlugin
{
    [BepInPlugin("dev.gamedoc.plugin.eoe", "Empire of Empires Plugin", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private bool castleButtonRemoved = false;

        private void Awake()
        {
            On.GameManager.Start += GameManager_Start;
            On.MenuManager.LateLoad += MenuManager_LateLoad;
            On.SaveManager.LoadBuildings += SaveManager_LoadBuildings;
            On.BuildingButton.Update += BuildingButton_Update;
            On.Tutorial.OnCastleTutorialFinished += Tutorial_OnCastleTutorialFinished;
            On.Tutorial.StartNightTutorial += Tutorial_StartNightTutorial;
            On.GameManager.Update += GameManager_Update;
            On.PlayerShooter.Shoot += PlayerShooter_Shoot;
            On.Upgrades.BuyZombieHp += Upgrades_BuyZombieHp;
            On.Enemy.TakeDamage += Enemy_TakeDamage;
        }

        private void Enemy_TakeDamage(On.Enemy.orig_TakeDamage orig, Enemy self, float dmg)
        {
            // fixes wrong enemy death counter

            if (self.hp < 0) return;
            orig.Invoke(self, dmg);
        }

        private void Tutorial_StartNightTutorial(On.Tutorial.orig_StartNightTutorial orig, Tutorial self)
        {
            // disables the night tutorial if the player has already seen it
            if (PlayerPrefs.GetInt("TUTORIAL") == 1) return;
            orig.Invoke(self);
        }

        private void Upgrades_BuyZombieHp(On.Upgrades.orig_BuyZombieHp orig, Upgrades self)
        {
            // because the button invokes the wrong method the "Building Heal" upgrade doesn't work
            // this works around the issue: if the player presses left shift while clicking the upgrade button it buys "Building Heal",
            // if not it buys "Zombie HP"

            if (Input.GetKey(KeyCode.LeftShift))
            {
                Upgrades._instance.BuyBuildingHeal();
            }
            else
            {
                orig.Invoke(self);
            }
        }

        private void PlayerShooter_Shoot(On.PlayerShooter.orig_Shoot orig, PlayerShooter self)
        {
            // don't shoot while the cursor is over the UI
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }
            }

            // don't shoot while placing buildings
            if (GameManager._instance.isBuildingUnderPlacement)
            {
                return;
            }

            orig.Invoke(self);
        }

        private void GameManager_Update(On.GameManager.orig_Update orig, GameManager self)
        {
            // add the ability to toggle time progression using the space bar
            orig.Invoke(self);

            if (Input.GetKeyUp(KeyCode.Space)) DayNightManager._instance.FastenTimeSpeed();
        }

        private void Tutorial_OnCastleTutorialFinished(On.Tutorial.orig_OnCastleTutorialFinished orig, Tutorial self)
        {
            // disable resource tutorial if player finished it before
            orig.Invoke(self);
            if (PlayerPrefs.GetInt("TUTORIAL") == 1)
            {
                Tutorial._instance.OnResourceTutorialFinished();
            }
        }

        private void BuildingButton_Update(On.BuildingButton.orig_Update orig, BuildingButton self)
        {
            // remove the castle button if castle has been already placed
            orig.Invoke(self);

            if (castleButtonRemoved) return;

            if (self.name == "Castle" && GameManager._instance.isCastlePlaced)
            {
                Destroy(self.gameObject);
                castleButtonRemoved = true;
            }
        }

        private void SaveManager_LoadBuildings(On.SaveManager.orig_LoadBuildings orig, SaveManager self)
        {
            int num = 0;
            while (PlayerPrefs.HasKey(SaveManager.building + num))
            {
                string[] array = PlayerPrefs.GetString(SaveManager.building + num).Split(new char[]
                {
            '|'
                });
                GameManager.BuildingType buildingType = SaveManager.ParseEnum<GameManager.BuildingType>(array[0]);
                foreach (BuildingButton buildingButton in Tutorial._instance.buildingButtons)
                {
                    if (buildingButton.type == buildingType)
                    {
                        // set the castle to already placed on savegame load
                        // to prevent the castle build button from appearing
                        if (buildingType == GameManager.BuildingType.CASTLE)
                        {
                            GameManager._instance.isCastlePlaced = true;
                        }
                        Building component = Instantiate(buildingButton.buildingPrefab).GetComponent<Building>();
                        component.buildingButton = buildingButton;
                        component.Init();
                        component.levelObjects[0].SetActive(false);
                        component.level = int.Parse(array[1]);
                        component.SetStats();
                        component.health = (float)int.Parse(array[2]);
                        component.transform.position = new Vector3(float.Parse(array[3]), float.Parse(array[4]), 0f);
                        component.SearchOccupiedTiles();
                        component.PlaceBuilding();
                        component.EnableUpgradesSprites(component.level - 1);
                    }
                }
                num++;
            }
        }

        private System.Collections.IEnumerator MenuManager_LateLoad(On.MenuManager.orig_LateLoad orig, MenuManager self)
        {
            // remove 3.5 second wait time
            SceneManager.LoadSceneAsync("Game");
            yield break;
        }

        private void GameManager_Start(On.GameManager.orig_Start orig, GameManager self)
        {
            orig.Invoke(self);

            // set controls to keyboard
            self.isMobile = false;

            // remove virtual joystick
            GameObject _go = FindObjectOfType<Joystick>().gameObject;
            Destroy(_go);
        }
    }
}