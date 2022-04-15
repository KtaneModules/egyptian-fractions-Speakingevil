using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EgyptianFracScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public List<KMSelectable> buttons;
    public TextMesh[] dispfrac;
    public TextMesh[] denoms;

    private int[] bound = new int[4] { 10, 30, 60, 100};
    private long[] frac = new long[4];
    private int[] gens = new int[4];
    private int[] s = new int[4] { 1, 1, 1, 1};
    private int subnum;
    private long check = 1;

    private static int moduleIDCounter;
    private int moduleID;
    private bool moduleSolved;

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        while(check == 1)
        {
            for (int i = 0; i < 4; i++)
                gens[i] = Random.Range(1, bound[i]);
            frac[0] = (gens[0] * ((gens[1] * (gens[2] + gens[3])) + (gens[2] * gens[3]))) + (gens[1] * gens[2] * gens[3]);
            frac[1] = gens[0] * gens[1] * gens[2] * gens[3];
            check = HCF(frac[0], frac[1]);
        }
        for(int i = 0; i < 2; i++)
        {
            frac[i] /= check;
            dispfrac[i].text = frac[i].ToString();
            for (int j = dispfrac[i].text.Length; j < 9; j++)
                dispfrac[i].text = "0" + dispfrac[i].text;
        }
        Debug.LogFormat("[Egyptian Fractions #{0}] The displayed fraction is {1}/{2}.", moduleID, frac[0], frac[1]);
        Debug.LogFormat("[Egyptian Fractions #{0}] Solution: 1/{1}", moduleID, string.Join(" + 1/", gens.OrderBy(x => x).Select(x => x.ToString()).ToArray()));
        foreach (KMSelectable button in buttons)
        {
            int b = buttons.IndexOf(button);
            button.OnInteract = delegate ()
            {
                if (!moduleSolved)
                {
                    switch (b)
                    {
                        case 3:
                            if (subnum > 0)
                            {
                                button.AddInteractionPunch(0.7f);
                                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, button.transform);
                                s[subnum] = 1;
                                denoms[subnum].text = "-";
                                subnum--;
                            }
                            break;
                        case 2:
                            button.AddInteractionPunch(0.7f);
                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, button.transform);
                            if (subnum > 2)
                            {
                                if (s.Any(x => x == 0))
                                {
                                    Debug.LogFormat("[Egyptian Fractions #{0}] Error: Division by zero.");
                                    Incorrect();
                                }
                                else
                                {
                                    Debug.LogFormat("[Egyptian Fractions #{0}] Submitted 1/{1}", moduleID, string.Join(" + 1/", s.Select(x => x.ToString()).ToArray()));
                                    frac[2] = (s[0] * ((s[1] * (s[2] + s[3])) + (s[2] * s[3]))) + (s[1] * s[2] * s[3]);
                                    frac[3] = s[0] * s[1] * s[2] * s[3];
                                    check = HCF(frac[2], frac[3]);
                                    for (int i = 2; i < 4; i++)
                                        frac[i] /= check;
                                    if (frac[0] == frac[2] && frac[1] == frac[3])
                                    {
                                        moduleSolved = true;
                                        module.HandlePass();
                                        Audio.PlaySoundAtTransform("Solve", transform);
                                        Debug.LogFormat("[Egyptian Fractions #{0}] = {1}/{2}. Correct.", moduleID, frac[2], frac[3]);
                                        dispfrac[0].text = new string[] { "EXCELLENT", "EXEMPLARY", "EMINENT", "ERUDITE", "ESTEEMED", "EXQUISITE", "EFFULGENT" }[Random.Range(0, 7)];
                                        dispfrac[1].text = new string[] { "FINDINGS", "FORMULA", "FACTORING", "FRUITION" }[Random.Range(0, 4)];
                                        for (int i = 0; i < 4; i++)
                                            denoms[i].text = "--";
                                    }
                                    else
                                    {
                                        Debug.LogFormat("[Egyptian Fractions #{0}] = {1}/{2}. Incorrect.", moduleID, frac[2], frac[3]);
                                        Incorrect();
                                    }
                                }
                            }
                            else
                            {
                                subnum++;
                                denoms[subnum].text = "01";
                            }
                            break;
                        default:
                            button.AddInteractionPunch(0.2f);
                            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, button.transform);
                            s[subnum] += b == 0 ? 10 : 1;
                            s[subnum] %= 100;
                            denoms[subnum].text = (s[subnum] < 10 ? "0" : "") + s[subnum].ToString();
                            break;
                    }
                }
                return false;
            };
        }
    }

    private long HCF(long a, long b)
    {
        while(a > 0 && b > 0)
        {
            if (a > b)
                a %= b;
            else
                b %= a;
        }
        return a | b;
    }

    private void Incorrect()
    {
        module.HandleStrike();
        subnum = 0;
        s = new int[4] { 1, 1, 1, 1 };
        denoms[0].text = "01";
        for(int i = 1; i < 4; i++)
            denoms[i].text = "-";
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} <#> [Enters a number into the denominator] | !{0} submit [Submits current entry/Moves to next entry] | !{0} back [Resets previous entry]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
        if(command == "submit")
        {
            buttons[2].OnInteract();
            yield break;
        }
        else if(command == "back")
        {
            buttons[3].OnInteract();
            yield break;
        }
        if (command.Length == 1)
            command = "0" + command;
        if (command.Length == 2 && command.All(x => x - '0' > -1 && x - '0' < 10))
        {
            while (s[subnum] % 10 != command[1] - '0')
            {
                buttons[1].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
            while (s[subnum] / 10 != command[0] - '0')
            {
                buttons[0].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
        }
        else
            yield return "sendtochaterror Invalid command. Enter \"submit\", \"back\", or a number below 100.";
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        int startIx;
        for (startIx = 0; startIx < 4; startIx++)
        {
            if (s[startIx] == gens[startIx])
                continue;
            goto wrong;
        }
        buttons[2].OnInteract();
        yield break;
        wrong:
        while (subnum != startIx)
        {
            if (subnum > startIx)
                buttons[3].OnInteract();
            else
                buttons[2].OnInteract();
            yield return new WaitForSeconds(0.05f);
        }
        for (int i = startIx; i < 4; i++)
        {
            while (s[i] % 10 != gens[i] % 10)
            {
                buttons[1].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
            while (s[i] / 10 != gens[i] / 10)
            {
                buttons[0].OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
            buttons[2].OnInteract();
            yield return new WaitForSeconds(0.05f);
        }
        // Debug.Log(gens.Join(" "));
        // yield break;
    }
}
