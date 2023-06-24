using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class JankHole : MonoBehaviour
{
    [SerializeField] private KMBombModule Module;
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMColorblindMode Colorblind;

    [SerializeField] private MeshRenderer HoleRenderer;
    [SerializeField] private TextMesh ColorBlindText;
    [SerializeField] private List<Material> JankHoleMaterials;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    string alphabet = "ZABCDEFGHIJKLMNOPQRSTUVWXY";
    List<Color> colorList = new List<Color> { Color.red, Color.green, Color.blue, Color.cyan, Color.magenta, Color.yellow, Color.white };
    List<string> shortColorNames = new List<string> { "R", "G", "B", "C", "M", "Y", "W" };
    List<string> fullColorNames = new List<string> { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow", "White" };

    string X, Y, Z;
    int[] colorIndexesArray = new int[11];

    int colorSequenceLength = 10;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
    }

    void Start()
    {
        ColorBlindText.gameObject.SetActive(Colorblind.ColorblindModeActive);
        CalculateGoalLetters();
        GenerateColors();
        HoleRenderer.material.color = colorList[colorIndexesArray[0]];

        Module.OnActivate += ModuleOnActivate;
    }

    void CalculateGoalLetters()
    {
        List<char> serialNumberLetters = Bomb.GetSerialNumberLetters().ToList<char>();
        switch (serialNumberLetters.Count)
        {
            case 2:
                X = serialNumberLetters[0].ToString();
                Y = serialNumberLetters[1].ToString();
                double ZValue = Math.Abs(alphabet.IndexOf(X) - alphabet.IndexOf(Y)) / 2.0;
                if (Bomb.GetOffIndicators().Count() > Bomb.GetOnIndicators().Count())
                {
                    ZValue = Math.Floor(ZValue);
                }
                else
                {
                    ZValue = Math.Ceiling(ZValue);
                }
                Z = alphabet[Convert.ToInt32(ZValue)].ToString();
                break;
            case 3:
                X = serialNumberLetters[0].ToString();
                Y = serialNumberLetters[1].ToString();
                Z = serialNumberLetters[2].ToString();
                break;
            case 4:
                X = alphabet[Math.Abs(serialNumberLetters[0] - serialNumberLetters[1])].ToString();
                Y = alphabet[Math.Abs(serialNumberLetters[1] - serialNumberLetters[2])].ToString();
                Z = alphabet[Math.Abs(serialNumberLetters[2] - serialNumberLetters[3])].ToString();
                break;
            default:
                break;
        }
        CustomLog($"Goal letters are {X}, {Y}, and {Z}.");
    }

    void GenerateColors()
    {
        int generatedColorIndex;
        int previousGeneratedIndex = colorList.Count;
        string colorLog = "";
        for (int i = 0; i < colorSequenceLength + 1; i++)
        {
            generatedColorIndex = Rnd.Range(0, colorList.Count);
            while (generatedColorIndex == previousGeneratedIndex)
            {
                generatedColorIndex = Rnd.Range(0, colorList.Count);
            }
            colorIndexesArray[i] = generatedColorIndex;
            previousGeneratedIndex = generatedColorIndex;
            colorLog += fullColorNames[colorIndexesArray[i]];
            if (i < colorSequenceLength - 1)
            {
                colorLog += ", ";
            }
            else if (i == colorSequenceLength - 1)
            {
                colorLog += " and ";
            }
            else
            {
                colorLog += ".";
            }
        }
        CustomLog($"Generated color sequence is: {colorLog}");
    }

    void CustomLog(string arg)
    {
        Debug.Log($"[Jank Hole #{ModuleId}] {arg}");
    }
	
    void ModuleOnActivate()
    {
        StartCoroutine("ColorCycle");
    }

    IEnumerator ColorCycle()
    {
        int currentIndex = -1;
        while (true)
        {
            for (int i = 0; i < colorIndexesArray.Length; ++i)
            {
                currentIndex = i;
                HoleRenderer.material = JankHoleMaterials[colorIndexesArray[i]];
                ColorBlindText.text = shortColorNames[colorIndexesArray[i]];
                yield return new WaitForSeconds(1);
            }
            currentIndex = -1;
            HoleRenderer.material = JankHoleMaterials[7];
            ColorBlindText.text = "";
            yield return new WaitForSeconds(2);
        }
    }
}