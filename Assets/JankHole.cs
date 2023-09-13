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
    List<string> shortColorNames = new List<string> { "R", "G", "B", "C", "M", "Y", "W" };
    List<string> fullColorNames = new List<string> { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow", "White" };

    string X, Y, Z;
    string KeyA, KeyB = "";
    int[] colorIndexesArray = new int[10];

    int colorSequenceLength = 9;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
    }

    void Start()
    {
        ColorBlindText.gameObject.SetActive(Colorblind.ColorblindModeActive);
        GenerateColors();
        CalculateGoalLetters();
        CalculateKeys();

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
        Log($"Goal letters are {X}, {Y}, and {Z}.");
    }

    void CalculateKeys()
    {
        for (int i = 0; i < 5; i++)
        {
            int currentColor = colorIndexesArray[i];
            switch (currentColor)
            {
                case 0:
                    if (Bomb.GetSerialNumber()[5] % 2 == 0)
                    {
                        KeyA += "EVEN";
                    }
                    else
                    {
                        KeyA += "ODD";
                    }
                    break;
                case 1:
                    if (!String.IsNullOrEmpty(KeyA))
                    {
                        KeyA = Rearrange(KeyA, true);
                    }
                    break;
                case 2:
                    if (!String.IsNullOrEmpty(KeyA))
                    {
                        KeyA = Reverse(KeyA.Substring(0, KeyA.Length / 2)) + KeyA.Substring(KeyA.Length / 2);
                    }
                    break;
                case 3:
                    if (!String.IsNullOrEmpty(KeyA))
                    {
                        KeyA = Reverse(KeyA);
                    }
                    break;
                case 4:
                    KeyA = AppendIndicators(KeyA, true);
                    break;
                case 5:
                    KeyA += X + Y + Z;
                    break;
                case 6:
                    List<char> serialNumLetters = Bomb.GetSerialNumberLetters().ToList();
                    for (int j = 0; j < serialNumLetters.Count; j++)
                    {
                        KeyA += serialNumLetters[j].ToString();
                    }
                    break;
            }
            if (!String.IsNullOrEmpty(KeyA))
            {
                Log($"Key A, Applied the {fullColorNames[currentColor]} color, Key A is now {KeyA}.");
            }
            else
            {
                Log($"Key A, Applied the {fullColorNames[currentColor]} color, Key A is now \"\".");
            }    
        }
        for (int i = 5; i < 10; i++)
        {
            int currentColor = colorIndexesArray[i];
            switch (currentColor)
            {
                case 0:
                    if (!String.IsNullOrEmpty(KeyB))
                    {
                        KeyB = Reverse(KeyB);
                    }
                    break;
                case 1:
                    if (!String.IsNullOrEmpty(KeyB))
                    {
                        KeyB = KeyB.Substring(0, KeyB.Length / 2) + Reverse(KeyB.Substring(KeyB.Length / 2));
                    }
                    break;
                case 2:
                    KeyB += KeyA;
                    break;
                case 3:
                    if (!String.IsNullOrEmpty(KeyB))
                    {
                        KeyB = Rearrange(KeyB, false);
                    }
                    break;
                case 4:
                    if (!String.IsNullOrEmpty(KeyA))
                    {
                        if (KeyA.Length % 2 == 1)
                        {
                            KeyB += KeyA[KeyA.Length / 2].ToString();
                        }
                        else
                        {
                            KeyB += KeyA[0].ToString();
                        }
                    }
                    break;
                case 5:
                    KeyB = AppendIndicators(KeyB, false);
                    break;
                case 6:
                    List<int> serialNumDigits = Bomb.GetSerialNumberNumbers().ToList();
                    for (int j = 0; j < serialNumDigits.Count; j++)
                    {
                        KeyB += alphabet[serialNumDigits[j]];
                    }
                    break;
            }
            if (!String.IsNullOrEmpty(KeyB))
            {
                Log($"Key B, Applied the {fullColorNames[currentColor]} color, Key B is now {KeyB}.");
            }
            else
            {
                Log($"Key B, Applied the {fullColorNames[currentColor]} color, Key B is now \"\".");
            }
        }

        KeyA += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        KeyB += "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string TempKA = KeyA;
        string TempKB = KeyB;
        KeyA = KeyB = "";

        for (int i = 0; i < TempKA.Length; i++)
        {
            if (!KeyA.Contains(TempKA[i]))
            {
                KeyA += TempKA[i];
            }
        }

        for (int i = 0; i < TempKB.Length; i++)
        {
            if (!KeyB.Contains(TempKB[i]))
            {
                KeyB += TempKB[i];
            }
        }
        Log($"After adding the alphabet and only keeping first occurrences of letters in each key, Key A is {KeyA} and Key B is {KeyB}.");

        for (int i = 0; i < 26; i++)
        {
            if (KeyA[i] == KeyB[i])
            {
                KeyB = KeyB.Substring(0, i) + alphabet[(alphabet.IndexOf(KeyB[i].ToString()) + 1) % 26] + KeyB.Substring(i + 1);
                Log($"Found duplicate letter {KeyA[i]} at position {i + 1}, new Key B is {KeyB}.");
            }
        }
        Log($"Final Key A and Key B are {KeyA} and {KeyB}.");
    }

    void GenerateColors()
    {
        int generatedColorIndex;
        int previousGeneratedIndex = shortColorNames.Count;
        string colorLog = "";
        for (int i = 0; i < colorSequenceLength + 1; i++)
        {
            generatedColorIndex = Rnd.Range(0, shortColorNames.Count);
            while (generatedColorIndex == previousGeneratedIndex)
            {
                generatedColorIndex = Rnd.Range(0, shortColorNames.Count);
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
        Log($"Generated color sequence is: {colorLog}");
    }

    void Log(string arg)
    {
        Debug.Log($"[Jank Hole #{ModuleId}] {arg}");
    }
	
    void ModuleOnActivate()
    {
        StartCoroutine("ColorCycle");
    }

    string Rearrange(string key, bool oddFirst)
    {
        List<char> keyEvenPositions = new List<char>();
        List<char> keyOddPositions = new List<char>();
        for (int j = 0; j < key.Length; j++)
        {
            if ((j + 1) % 2 == 0)
            {
                keyEvenPositions.Add(key[j]);
            }
            else
            {
                keyOddPositions.Add(key[j]);
            }
        }
        key = "";
        if (oddFirst)
        {
            for (int j = 0; j < keyOddPositions.Count; j++)
            {
                key += keyOddPositions[j].ToString();
            }
            for (int j = 0; j < keyEvenPositions.Count; j++)
            {
                key += keyEvenPositions[j].ToString();
            }
        }
        else
        {
            for (int j = 0; j < keyEvenPositions.Count; j++)
            {
                key += keyEvenPositions[j].ToString();
            }
            for (int j = 0; j < keyOddPositions.Count; j++)
            {
                key += keyOddPositions[j].ToString();
            }
        }
        return key;
    }

    string Reverse(string key)
    {
        char[] charArray = key.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }
    
    string AppendIndicators(string key, bool lit)
    {
        List<string> indicatorList = new List<string>();
        if (lit)
        {
            indicatorList = Bomb.GetOnIndicators().ToList();
        }
        else
        {
            indicatorList = Bomb.GetOffIndicators().ToList();
        }
        indicatorList.Sort();
        for (int j = 0; j < indicatorList.Count; j++)
        {
            key += indicatorList[j];
        }
        return key;
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