using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

//�� ���ӿ����� �Ÿ��� ����� ������ ũ�⸦ ������ �ʿ䰡 ���⿡ �ϳ��� AudioSource�� AudioClip���� �������� �����ų ���̴�.
//��������� ������ AudioSource�� ȿ������ ������ AudioSource�� SoundManager�� �ڽ� ������Ʈ�� ����


public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;

    public static SoundManager Instance
    {
        get
        {

            if (instance == null)
            {
                instance = FindObjectOfType<SoundManager>();
            }

            return instance;
        }
    } // Sound�� �������ִ� ��ũ��Ʈ�� �ϳ��� �����ؾ��ϰ� instance������Ƽ�� ���� ��𿡼��� �ҷ��������� �̱��� ���

    private AudioSource bgmPlayer;
    private AudioSource sfxPlayer;

    public float masterVolumeSFX = 1f;
    public float masterVolumeBGM = 1f;        

    private AudioClip randomBgmClip;

    [SerializeField]
    private AudioClip[] sfxAudioClips; //ȿ������ ����

    Dictionary<string, AudioClip> audioClipsDic = new Dictionary<string, AudioClip>(); //ȿ���� ��ųʸ�
    // AudioClip�� Key,Value ���·� �����ϱ� ���� ��ųʸ� ���

    private void Awake()
    {
        //if (Instance != this)
        //{
        //    Destroy(this.gameObject);
        //}
        //DontDestroyOnLoad(this.gameObject); //���� ������ ����� ��.

        bgmPlayer = GameObject.Find("BGMSoundPlayer").GetComponent<AudioSource>();
        sfxPlayer = GameObject.Find("SFXSoundPlayer").GetComponent<AudioSource>();

        int randomBgmNumber = Random.Range(1, 4);       
        randomBgmClip = Resources.Load("GameSound/Background_Music_" + randomBgmNumber.ToString()) as AudioClip;

        foreach (AudioClip audioclip in sfxAudioClips)
        {
            audioClipsDic.Add(audioclip.name, audioclip);
        }

        // Start Background Music
        //PlayBGMSound(1f);
    }

    // ȿ�� ���� ��� : �̸��� �ʼ� �Ű�����, ������ ������ �Ű������� ����
    public void PlaySFXSound(string name, float volume = 1f)
    {
        if (audioClipsDic.ContainsKey(name) == false)
        {
            Debug.Log(name + " is not Contained audioClipsDic");
            return;
        }
        sfxPlayer.PlayOneShot(audioClipsDic[name], volume * masterVolumeSFX);
    }

    //BGM ���� ��� : ������ ������ �Ű������� ����
    public void PlayBGMSound(float volume = 1f)
    {
        bgmPlayer.loop = true; //BGM �����̹Ƿ� ��������
        bgmPlayer.volume = volume * masterVolumeBGM;

        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            bgmPlayer.clip = randomBgmClip;
            bgmPlayer.Play();
        }   

        //���� ���� �´� BGM ���
    }
}