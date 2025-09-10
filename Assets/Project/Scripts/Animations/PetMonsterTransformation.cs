using System.Collections;
using UnityEngine;
using PlayMySpace.PMSC.Managers;
using PlayMySpace.PMSC.Input;
using Mirror;

/// <summary>
/// PetMonsterTransformAnimation.cs
/// 
/// This script contains the logic for the animation that occurs when the Pet transforms into the PetMonster and vice versa.
/// 
/// Copyright © 2021 Play My Space
/// </summary>
public class PetMonsterTransformation : NetworkBehaviour
{
    #region Class Members
    [SerializeField] private bool executePetToMonsterTransformation = false;
    [SerializeField] private bool executeMonsterToPetTransformation = false;
    [Space(10)]

    [SerializeField] private GameObject[] petToMonsterEffects; // The sequence of particle effects that will appear during the transformation from Pet to PetMonster
    [SerializeField] private GameObject[] monsterToPetEffects; // The sequence of particle effects that will appear during the transformation from PetMonster to Pet

    private CameraController cameraController;
    private NetworkedPetController pet; // The Pet GameObject that will "transform" into the PetMonster
    private PetMonsterController monster; // The PetMonster GameObject that will "transform" into the Pet

    bool localPlayer = false;
    #endregion

    #region Class Accessors
    public PetMonsterController Monster
    {
        get { return monster; }
        set { monster = value; }
    }
    #endregion

    #region MonoBehaviour Stuff
    private void Awake()
    {
    }
    #endregion

    #region Class Implementation - Private
    private IEnumerator PetToMonsterTransformationCoroutine()
    {
        if (localPlayer)
        {
            GameManager.Instance.PlayerLogicManager.PlayerControlEnabled = false;
        }

        // First rotate Pet towards the camera while playing the SurprisedJump animation
        pet.GetComponent<Rigidbody>().useGravity = false;
        pet.Animator.SetTrigger("SurprisedJump");
        Vector3 rotationTowardsCamera = Quaternion.LookRotation(Camera.main.transform.position - pet.transform.position).eulerAngles;
        rotationTowardsCamera = new Vector3(0, rotationTowardsCamera.y, 0);
        float t = 0;
        float lerpSpeed = 2.5f;

        while (t < 1.25f)
        {
            if (Mathf.Abs(pet.PetModel.transform.eulerAngles.y - rotationTowardsCamera.y) > 0.1f)
            {
                pet.PetModel.transform.eulerAngles = Vector3.Lerp(pet.PetModel.transform.rotation.eulerAngles, rotationTowardsCamera, t);
            }

            t += Time.fixedDeltaTime * lerpSpeed;
            yield return new WaitForFixedUpdate();
        }

        GameObject petMonsterTransformationOrb = Instantiate(petToMonsterEffects[0], pet.transform.position + new Vector3(0, 0.5f, 0), Quaternion.identity, pet.transform);
        t = 0;
        float lerpTime = 0.15f;

        while (petMonsterTransformationOrb.transform.localScale.x < 10)
        {
            float interpolValue = t / lerpTime;
            Vector3 orbSizeDelta = new Vector3(interpolValue, interpolValue, interpolValue);
            petMonsterTransformationOrb.transform.localScale = orbSizeDelta;

            foreach (Transform tr in petMonsterTransformationOrb.GetComponentInChildren<Transform>())
            {
                tr.localScale = orbSizeDelta;
            }

            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        float time = 0;
        t = 0;
        float t2 = 0;
        lerpTime = 5;
        float lerpSpeedLerp = 0;
        Vector3 startPosition = pet.transform.position;
        Vector3 destination = new Vector3(pet.transform.position.x, 120, pet.transform.position.z);

        if (localPlayer)
        {
            cameraController.ZoomControl = false;
        }

        while (t < 0.99f)
        {
            if (localPlayer)
            {
                cameraController.ZoomLevel = Mathf.Lerp(cameraController.ZoomLevel, -1, t);
            }

            pet.transform.position = Vector3.Lerp(startPosition, destination, t);

            if (pet.PetModel.transform.localScale.x > 0)
            {
                pet.PetModel.transform.localScale = new Vector3(pet.PetModel.transform.localScale.x - time * 0.025f,
                                                                pet.PetModel.transform.localScale.y - time * 0.025f,
                                                                pet.PetModel.transform.localScale.z - time * 0.025f);
                pet.PetModel.transform.position += new Vector3(0, time * 0.0125f, 0);
            }
            else if (pet.PetModel.transform.localScale.x < 0)
            {
                pet.PetModel.transform.localScale = Vector3.zero;
            }

            pet.PetModel.transform.rotation = Quaternion.Euler(pet.PetModel.transform.rotation.eulerAngles + new Vector3(0,  t2, 0));

            time += Time.fixedDeltaTime;
            t = time / lerpTime;
            t = t * t * (3f - 2f * t);
            t2 += Time.fixedDeltaTime * Mathf.Lerp(0, 120, lerpSpeedLerp);
            lerpSpeedLerp += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        pet.transform.position = new Vector3(pet.transform.position.x, 120, pet.transform.position.z);
        pet.PetModel.transform.localPosition = Vector3.zero;
        pet.GetComponent<CapsuleCollider>().enabled = false;

        t = 10;
        lerpSpeed = 10;

        while (petMonsterTransformationOrb.transform.localScale.x > 0)
        {
            Vector3 sizeDelta = new Vector3(t, t, t);
            petMonsterTransformationOrb.transform.localScale = sizeDelta;

            foreach (Transform tr in petMonsterTransformationOrb.GetComponentInChildren<Transform>())
            {
                tr.localScale = sizeDelta;
            }

            t -= Time.fixedDeltaTime * lerpSpeed;
            yield return new WaitForFixedUpdate();
        }

        Destroy(petMonsterTransformationOrb);

        GameObject petMonsterTransformationBang = Instantiate(petToMonsterEffects[1], pet.transform.position + new Vector3(0, 2, 0), Quaternion.identity, null);

        if (localPlayer)
        {
            monster = GameManager.Instance.PlayerLogicManager.Monster.GetComponent<PetMonsterController>();
            monster.transform.position = new Vector3(pet.transform.position.x, 120, pet.transform.position.z);
            monster.PetMonsterModel.transform.rotation = Quaternion.Euler(rotationTowardsCamera);
            monster.PetMonsterModel.transform.localScale = Vector3.zero;
            pet.ChangeCharacterActiveCmd(GameManager.Instance.PlayerLogicManager.Monster, true);

            t = 0;
            lerpSpeed = 10;
            while (monster.PetMonsterModel.transform.localScale.x < 1)
            {
                monster.PetMonsterModel.transform.localScale = new Vector3(t, t, t);
                t += Time.fixedDeltaTime * lerpSpeed;
                yield return new WaitForFixedUpdate();
            }

            monster.PetMonsterModel.transform.localScale = Vector3.one;

            GameManager.Instance.PlayerLogicManager.petMode = PlayerLogicManager.PetMode.Monster;
            GameManager.Instance.PlayerLogicManager.WorldLight.transform.position = monster.transform.position;
            GameManager.Instance.PlayerLogicManager.WorldLight.transform.parent = monster.transform;
            GameManager.Instance.PlayerLogicManager.PlayerControlEnabled = true;
            cameraController.SwitchTarget(monster.gameObject);
        }

        pet.gameObject.SetActive(false);

        yield return new WaitForSeconds(10);

        Destroy(petMonsterTransformationBang);
        Destroy(gameObject);
    }

    private IEnumerator MonsterToPetTransformationCoroutine()
    {
        if (localPlayer)
        {
            GameManager.Instance.PlayerLogicManager.PlayerControlEnabled = false;
        }

        Vector3 rotationTowardsCamera = Quaternion.LookRotation(Camera.main.transform.position - monster.transform.position).eulerAngles;
        rotationTowardsCamera = new Vector3(0, rotationTowardsCamera.y, 0);
        float t = 0;
        float lerpSpeed = 2.5f;

        while (t < 1.25f)
        {
            if (Mathf.Abs(monster.PetMonsterModel.transform.eulerAngles.y - rotationTowardsCamera.y) > 0.1f)
            {
                monster.PetMonsterModel.transform.eulerAngles = Vector3.Lerp(monster.PetMonsterModel.transform.rotation.eulerAngles, rotationTowardsCamera, t);
            }

            t += Time.fixedDeltaTime * lerpSpeed;
            yield return new WaitForFixedUpdate();
        }

        GameObject monsterToPetTransformationOrb = Instantiate(monsterToPetEffects[0], monster.transform.position + new Vector3(0, 1.8f, 0), Quaternion.identity, monster.transform);
        monsterToPetTransformationOrb.transform.localScale = Vector3.zero;

        t = 0;
        lerpSpeed = 15;

        while (monsterToPetTransformationOrb.transform.localScale.x < 30)
        {
            Vector3 orbSizeDelta = new Vector3(t, t, t);
            monsterToPetTransformationOrb.transform.localScale = orbSizeDelta;

            foreach (Transform tr in monsterToPetTransformationOrb.GetComponentInChildren<Transform>())
            {
                tr.localScale = orbSizeDelta;
            }

            t += Time.fixedDeltaTime * lerpSpeed;
            yield return new WaitForFixedUpdate();
        }

        float time = 0;
        t = 0;
        float t2 = 0;
        float lerpTime = 5;
        float lerpSpeedLerp = 0;
        Vector3 startPosition = monster.transform.position;
        Vector3 destination = new Vector3(monster.transform.position.x, -0.5f, monster.transform.position.z);

        while (t < 1)
        {
            if (localPlayer)
            {
                cameraController.ZoomLevel = Mathf.Lerp(cameraController.ZoomLevel, 0, t);
            }

            monster.transform.position = Vector3.Lerp(startPosition, destination, t);

            if (monster.PetMonsterModel.transform.localScale.x > 0)
            {
                monster.PetMonsterModel.transform.localScale = new Vector3(monster.PetMonsterModel.transform.localScale.x - time * 0.0125f,
                                                                           monster.PetMonsterModel.transform.localScale.y - time * 0.0125f,
                                                                           monster.PetMonsterModel.transform.localScale.z - time * 0.0125f);
                monster.PetMonsterModel.transform.position += new Vector3(0, time * 0.025f, 0);
            }
            else if (monster.PetMonsterModel.transform.localScale.x < 0)
            {
                monster.PetMonsterModel.transform.localScale = Vector3.zero;
            }

            monster.PetMonsterModel.transform.rotation = Quaternion.Euler(monster.PetMonsterModel.transform.rotation.eulerAngles + new Vector3(0, t2, 0));

            time += Time.fixedDeltaTime;
            t = time / lerpTime;
            t = t * t * (3f - 2f * t);
            t2 += Time.fixedDeltaTime * Mathf.Lerp(0, 120, lerpSpeedLerp);
            lerpSpeedLerp += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        monster.transform.position = new Vector3(monster.transform.position.x, -0.5f, monster.transform.position.z);
        monster.PetMonsterModel.transform.localPosition = new Vector3(0, -0.1f, 0);

        t = 10;
        lerpSpeed = 30;

        while (monsterToPetTransformationOrb.transform.localScale.x > 0)
        {
            Vector3 sizeDelta = new Vector3(t, t, t);
            monsterToPetTransformationOrb.transform.localScale = sizeDelta;

            foreach (Transform tr in monsterToPetTransformationOrb.GetComponentInChildren<Transform>())
            {
                tr.localScale = sizeDelta;
            }

            t -= Time.fixedDeltaTime * lerpSpeed;
            yield return new WaitForFixedUpdate();
        }

        Destroy(monsterToPetTransformationOrb);

        GameObject monsterToPetTransformationBang = Instantiate(monsterToPetEffects[1], new Vector3(monster.transform.position.x, 0, monster.transform.position.z), Quaternion.identity, null);

        if (localPlayer)
        {
            pet = GameManager.Instance.PlayerLogicManager.Pet.GetComponent<NetworkedPetController>();
            pet.transform.position = new Vector3(monster.transform.position.x, 0, monster.transform.position.z);
            pet.PetModel.transform.rotation = Quaternion.Euler(rotationTowardsCamera);
            pet.GetComponent<CapsuleCollider>().enabled = true;
            pet.GetComponent<Rigidbody>().useGravity = true;
            pet.ChangeCharacterActiveCmd(pet.gameObject, true);

            t = 0;
            lerpSpeed = 10;

            while (pet.PetModel.transform.localScale.x < 1)
            {
                pet.PetModel.transform.localScale = new Vector3(t, t, t);
                t += Time.fixedDeltaTime * lerpSpeed;
                yield return new WaitForFixedUpdate();
            }

            pet.PetModel.transform.localScale = Vector3.one;
            GameManager.Instance.PlayerLogicManager.petMode = PlayerLogicManager.PetMode.Pet;
            GameManager.Instance.PlayerLogicManager.WorldLight.transform.position = pet.transform.position;
            GameManager.Instance.PlayerLogicManager.WorldLight.transform.parent = pet.transform;
            GameManager.Instance.PlayerLogicManager.PlayerControlEnabled = true;
            cameraController.SwitchTarget(pet.gameObject);
            cameraController.ZoomControl = true;
        }

        yield return new WaitForSeconds(5);

        Destroy(monsterToPetTransformationBang);
        Destroy(monster.gameObject);
        Destroy(gameObject);
    }
    #endregion

    #region Class Implementation - Public
    public void StartTransformation(GameObject g)
    {
        cameraController = GameManager.Instance.CameraController;

        if (executePetToMonsterTransformation)
        {
            pet = g.GetComponent<NetworkedPetController>();
            localPlayer = g.GetComponent<NetworkIdentity>().isLocalPlayer;

            if (monster == null)
            {
                if (localPlayer)
                {
                    pet.ReplacePetWithMonsterCmd();
                }
            }

            PetToMonsterTransformation();
        }
        else if (executeMonsterToPetTransformation)
        {
            monster = g.GetComponent<PetMonsterController>();
            localPlayer = g.GetComponent<NetworkIdentity>().isLocalPlayer;

            if (localPlayer)
            {
                monster.ReplaceMonsterWithPetCmd(GameManager.Instance.PlayerLogicManager.Pet);
            }

            MonsterToPetTransformation();
        }
    }

    public void PetToMonsterTransformation()
    {
        StartCoroutine(PetToMonsterTransformationCoroutine());
    }

    public void MonsterToPetTransformation()
    {
        StartCoroutine(MonsterToPetTransformationCoroutine());
    }
    #endregion
}
