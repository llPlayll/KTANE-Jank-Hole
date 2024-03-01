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

    [SerializeField] private KMSelectable JankHoleSelectable;
    [SerializeField] private MeshRenderer HoleRenderer;
    [SerializeField] private TextMesh ColorBlindText;
    [SerializeField] private TextMesh InputText;
    [SerializeField] private TextMesh DebugInputText;
    [SerializeField] private List<Material> JankHoleMaterials;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    string alphabet = "ZABCDEFGHIJKLMNOPQRSTUVWXY";
    List<string> shortColorNames = new List<string> { "R", "G", "B", "C", "M", "Y", "W" };
    List<string> fullColorNames = new List<string> { "Red", "Green", "Blue", "Cyan", "Magenta", "Yellow", "White" };

    string X, Y, Z;
    string KeyA, KeyB;
    string SolutionCode;
    List<string> BinaryGrid = new List<string>() { "", "", "" };
    List<string> ZerosGrid = new List<string>() { "", "", "" };
    List<string> OnesGrid = new List<string>() { "", "", "" };
    int[] colorIndexesArray = new int[10];

    bool isHolding;
    string input;
    int digitsEntered;

    private int lastSolved;
    bool isReadyForSkip;

    bool colorSequenceBreak;
    bool TPCommandOver;
    int globalColorSequenceIdx;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        JankHoleSelectable.OnInteract += delegate () { HoleOnInteract(); return false; };
        JankHoleSelectable.OnInteractEnded += delegate () { HoleOnInteractEnded(); };
    }

    void Start()
    {
        ColorBlindText.gameObject.SetActive(Colorblind.ColorblindModeActive);

        GenerateColors();
        CalculateGoalLetters();
        CalculateKeys();
        CreateGrids();
        GenerateSolution();

        Module.OnActivate += ModuleOnActivate;
    }

    void HoleOnInteract()
    {
        if (ModuleSolved)
        {
            return;
        }
        if (!isHolding)
        {
            JankHoleSelectable.AddInteractionPunch();
            input += "[";
            isHolding = true;
        }
    }

    void HoleOnInteractEnded()
    {
        if (ModuleSolved)
        {
            return;
        }
        if (isHolding)
        {
            JankHoleSelectable.AddInteractionPunch();
            input += "]";
            isHolding = false;
        }
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

    void CreateGrids()
    {
        BinaryGrid[0] = LetterToBinaryMorse(X);
        BinaryGrid[1] = LetterToBinaryMorse(Y);
        BinaryGrid[2] = LetterToBinaryMorse(Z);

        int maxLength = new[] { BinaryGrid[0].Length, BinaryGrid[1].Length, BinaryGrid[2].Length }.Max();

        BinaryGrid[0] += new string('0', (maxLength - BinaryGrid[0].Length));
        BinaryGrid[1] += new string('0', (maxLength - BinaryGrid[1].Length));
        BinaryGrid[2] += new string('0', (maxLength - BinaryGrid[2].Length));

        Log($"Binary grid rows are {BinaryGrid[0]} {BinaryGrid[1]} and {BinaryGrid[2]}.");

        int binaryGridSize = maxLength * 3;

        string TempKA = KeyA;
        while (TempKA.Length < binaryGridSize)
        {
            TempKA += KeyA;
        }
        ZerosGrid[0] = TempKA.Substring(0, maxLength);
        ZerosGrid[1] = TempKA.Substring(maxLength, maxLength);
        ZerosGrid[2] = TempKA.Substring(maxLength * 2, maxLength);

        Log($"Zeros grid rows are {ZerosGrid[0]} {ZerosGrid[1]} and {ZerosGrid[2]}.");

        string TempKB = KeyB;
        while (TempKB.Length < binaryGridSize)
        {
            TempKB += KeyB;
        }
        OnesGrid[0] = TempKB.Substring(0, maxLength);
        OnesGrid[1] = TempKB.Substring(maxLength, maxLength);
        OnesGrid[2] = TempKB.Substring(maxLength * 2, maxLength);

        Log($"Ones grid rows are {OnesGrid[0]} {OnesGrid[1]} and {OnesGrid[2]}.");
    }

    void GenerateSolution()
    {
        for (int i = 0; i < BinaryGrid[0].Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (BinaryGrid[j][i] == '0')
                {
                    SolutionCode += ZerosGrid[j][i];
                }
                else
                {
                    SolutionCode += OnesGrid[j][i];
                }
            }
        }
        Log($"The full solution code is {SolutionCode}.");
        if (SolutionCode.Length > 8)
        {
            SolutionCode = SolutionCode.Substring(0, 8);
            Log($"The solution code will be cut to finally be {SolutionCode}.");
        }
    }

    void GenerateColors()
    {
        int generatedColorIndex;
        int previousGeneratedIndex = shortColorNames.Count;
        string colorLog = "";
        for (int i = 0; i < 10; i++)
        {
            generatedColorIndex = Rnd.Range(0, shortColorNames.Count);
            while (generatedColorIndex == previousGeneratedIndex)
            {
                generatedColorIndex = Rnd.Range(0, shortColorNames.Count);
            }
            colorIndexesArray[i] = generatedColorIndex;
            previousGeneratedIndex = generatedColorIndex;
            colorLog += fullColorNames[colorIndexesArray[i]];
            if (i < 8)
            {
                colorLog += ", ";
            }
            else if (i == 8)
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

    string LetterToBinaryMorse(string letter)
    {
        switch (letter)
        {
            case "A":
                return "10111";
            case "B":
                return "111010101";
            case "C":
                return "11101011101";
            case "D":
                return "1110101";
            case "E":
                return "1";
            case "F":
                return "101011101";
            case "G":
                return "111011101";
            case "H":
                return "1010101";
            case "I":
                return "101";
            case "J":
                return "1011101110111";
            case "K":
                return "111010111";
            case "L":
                return "101110101";
            case "M":
                return "1110111";
            case "N":
                return "11101";
            case "O":
                return "11101110111";
            case "P":
                return "10111011101";
            case "Q":
                return "1110111010111";
            case "R":
                return "1011101";
            case "S":
                return "10101";
            case "T":
                return "111";
            case "U":
                return "1010111";
            case "V":
                return "101010111";
            case "W":
                return "101110111";
            case "X":
                return "11101010111";
            case "Y":
                return "1110101110111";
            case "Z":
                return "11101110101";
            default:
                return "";
        }
    }

    string GestureToLetter(string gesture)
    {
        switch (gesture)
        {
            case "[][pp]":
                return "A";
            case "[pp]p[][]":
                return "B";
            case "[][]":
                return "C";
            case "[pp][]p[]":
                return "D";
            case "[]p[][]p[]":
                return "E";
            case "[]p[pp][]":
                return "F";
            case "[]p[pp]":
                return "G";
            case "[][]p[p]":
                return "H";
            case "[][p]p[][]":
                return "I";
            case "[p]p[]":
                return "J";
            case "[p]p[][]":
                return "K";
            case "[]p[][]":
                return "L";
            case "[][]p[][]":
                return "M";
            case "[p][]p[][]":
                return "N";
            case "[]p[]p[]p[]":
                return "O";
            case "[][]p[]p[]":
                return "P";
            case "[][]p[]":
                return "Q";
            case "[]p[][pp]":
                return "R";
            case "[]p[]p[][]":
                return "S";
            case "[][p]":
                return "T";
            case "[]p[]p[p]":
                return "U";
            case "[][p][]":
                return "V";
            case "[]p[p][]":
                return "W";
            case "[p][]":
                return "X";
            case "[p][]p[]":
                return "Y";
            case "[]p[]p[]":
                return "Z";
            case "[ppp]":
                return digitsEntered.ToString();
            default:
                return "?";
        }
    }

    string LetterToGesture(string letter)
    {
        switch (letter)
        {
            case "A":
                return "[][pp]";
            case "B":
                return "[pp]p[][]";
            case "C":
                return "[][]";
            case "D":
                return "[pp][]p[]";
            case "E":
                return "[]p[][]p[]";
            case "F":
                return "[]p[pp][]";
            case "G":
                return "[]p[pp]";
            case "H":
                return "[][]p[p]";
            case "I":
                return "[][p]p[][]";
            case "J":
                return "[p]p[]";
            case "K":
                return "[p]p[][]";
            case "L":
                return "[]p[][]";
            case "M":
                return "[][]p[][]";
            case "N":
                return "[p][]p[][]";
            case "O":
                return "[]p[]p[]p[]";
            case "P":
                return "[][]p[]p[]";
            case "Q":
                return "[][]p[]";
            case "R":
                return "[]p[][pp]";
            case "S":
                return "[]p[]p[][]";
            case "T":
                return "[][p]";
            case "U":
                return "[]p[]p[p]";
            case "V":
                return "[][p][]";
            case "W":
                return "[]p[p][]";
            case "X":
                return "[p][]";
            case "Y":
                return "[p][]p[]";
            case "Z":
                return "[]p[]p[]";
            default:
                return "";
        }
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

    void checkInput()
    {
        string enteredLetter = GestureToLetter(input);
        InputText.text = enteredLetter;
        if (enteredLetter == "?")
        {
            Log($"Submitted gesture is invalid, strike!");
            Module.HandleStrike();
        }
        else if (alphabet.Contains(enteredLetter))
        {
            if (SolutionCode[0].ToString() == enteredLetter)
            {
                SolutionCode = SolutionCode.Substring(1);
                digitsEntered++;
                if (SolutionCode.Length > 0)
                {
                    Log($"Submitted letter {enteredLetter} is correct, new solution code is {SolutionCode}.");
                    isReadyForSkip = true;
                }
                else
                {
                    Log($"Submitted letter {enteredLetter} is correct, all letters were entered, module solved!");
                    ModuleSolved = true;
                    Module.HandlePass();
                    StopCoroutine("ColorCycle");
                    InputText.text = "";
                    HoleRenderer.material = JankHoleMaterials[1];
                    ColorBlindText.text = "!";
                }
            }
            else
            {
                Log($"Submitted letter {enteredLetter} is incorrect, strike!");
                Module.HandleStrike();
            }
        }
        else
        {
            Log($"The gesture [ppp] was entered, showing the number of letters that was entered, which is {digitsEntered}");
        }
    }

    IEnumerator ColorCycle()
    {
        while (true)
        {
            for (int i = 0; i < colorIndexesArray.Length; ++i)
            {
                globalColorSequenceIdx = i;
                HoleRenderer.material = JankHoleMaterials[colorIndexesArray[i]];
                ColorBlindText.text = shortColorNames[colorIndexesArray[i]];
                if (!String.IsNullOrEmpty(input))
                {
                    input += "p";
                }
                yield return new WaitForSeconds(1);
            }
            colorSequenceBreak = true;
            HoleRenderer.material = JankHoleMaterials[7];
            ColorBlindText.text = "";
            if (!String.IsNullOrEmpty(input))
            {
                if (isHolding)
                {
                    Log("The hole is still held on the break, clearing input and forcing a release.");
                    input = "";
                    isHolding = false;
                }
                else
                {
                    input = input.Trim('p');
                    Log($"Submitted {input}, checking submission.");
                    checkInput();
                    input = "";
                }
            }
            yield return new WaitForSeconds(2);
            InputText.text = "";
            colorSequenceBreak = false;
        }
    }

    private void Update()
    {
        DebugInputText.text = input;
        var solvedCount = Bomb.GetSolvedModuleNames().Where(x => x != "Jank Hole").Count();
        if (solvedCount != lastSolved)
        {
            lastSolved = solvedCount;
            if (isReadyForSkip)
            {
                isReadyForSkip = false;
                if (SolutionCode.Length > 3)
                {
                    digitsEntered += 3;
                    SolutionCode = SolutionCode.Substring(3);
                    Log($"A module has been solved in between entering letters, skipping next 3 letters, new solution code is {SolutionCode}");
                }
                else
                {
                    digitsEntered += SolutionCode.Length - 1;
                    SolutionCode = SolutionCode.Substring(SolutionCode.Length - 1);
                    Log($"A module has been solved in between entering letters, skipping to the last letter as the solution code is less than 3 letters long, new solution code is {SolutionCode}");
                }
            }
        }
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} [/p/] to execute a gesture, where a [ is a press, a p is waiting for a color switch, and ] is a release. !{0} cb to toggle colourblind mode.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        Command = Command.ToLowerInvariant();
        if (Command == "cb")
        {
            yield return null;
            ColorBlindText.gameObject.SetActive(!ColorBlindText.gameObject.activeSelf);
        }
        else if (Command.Replace("p", "").Replace("[", "").Replace("]", "") != "")
        {
            yield return "sendtochaterror Invalid command!";
        }
        else
        {
            int squareBrackets = 0;
            for (int i = 0; i < Command.Length; i++)
            {
                if (Command[i] == '[')
                {
                    squareBrackets++;
                }
                else if (Command[i] == ']')
                {
                    squareBrackets--;
                }
            }
            if (squareBrackets != 0)
            {
                yield return "sendtochaterror Invalid gesture: more holds than releases / more releases than holds!";
            }

            Command = Command.TrimStart('p').TrimEnd('p');
            if (Command.Count(p => p == 'p') > 10)
            {
                yield return "sendtochaterror Invalid gesture: too many color switches!";
            }

            bool tpCommandIsHolding = false;
            for (int i = 0; i < Command.Length; i++)
            {
                if (Command[i] == '[')
                {
                    if (tpCommandIsHolding)
                    {
                        yield return "sendtochaterror Invalid gesture: a hold inside of another hold!";
                    }
                    else
                    {
                        tpCommandIsHolding = true;
                    }
                }
                else if (Command[i] == ']')
                {
                    if (!tpCommandIsHolding)
                    {
                        yield return "sendtochaterror Invalid gesture: a release without a hold!";
                    }
                    else
                    {
                        tpCommandIsHolding = false;
                    }
                }
            }

            yield return null;
            yield return new WaitUntil(() => colorSequenceBreak);
            for (int i = 0; i < Command.Length; i++)
            {
                switch (Command[i].ToString())
                {
                    case "[":
                        JankHoleSelectable.OnInteract();
                        break;
                    case "]":
                        JankHoleSelectable.OnInteractEnded();
                        break;
                    case "p":
                        int startIdx = globalColorSequenceIdx;
                        yield return new WaitUntil(() => startIdx != globalColorSequenceIdx);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        while (SolutionCode.Length > 0)
        {
            yield return new WaitUntil(() => colorSequenceBreak);
            if (SolutionCode.Length > 0)
            {
                StartCoroutine(ProcessTwitchCommand(LetterToGesture(SolutionCode[0].ToString())));
                yield return new WaitForSeconds(3f);
            }
        }
    }
}