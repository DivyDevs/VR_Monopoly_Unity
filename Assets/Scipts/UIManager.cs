

using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject resultContainer;
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private List<GameObject> resultTextBoxes;

    private void Awake()
    {
        resultTextBoxes = new List<GameObject>();
    }

    private void OnEnable()
    {
        DiceRoller.OnDiceRoll += setDiceUI;
        Dice.OnDiceResult += SetText;
        PhysicalDice.OnPhysicalDiceResult += SetPhysicalText;
    }

    private void OnDisable()
    {
        DiceRoller.OnDiceRoll -= setDiceUI;
        Dice.OnDiceResult -= SetText;
        PhysicalDice.OnPhysicalDiceResult -= SetPhysicalText;
    }

    private void setDiceUI(int _diceRolled)
    {
        foreach (var textBox in resultTextBoxes)
        {
            Destroy(textBox);
        }

        resultTextBoxes = new List<GameObject>();

        for (int i = 0; i < _diceRolled; i++)
        {
            resultTextBoxes.Add(Instantiate(textPrefab, resultContainer.transform));
        }
    }

    private void SetText(int _diceIndex, int _diceResult)
    {
        if (_diceIndex < 0 || _diceIndex >= resultTextBoxes.Count) { return; }

        TMP_Text text = resultTextBoxes[_diceIndex].GetComponent<TMP_Text>();
        if (text != null) { text.text = $"Dice {_diceIndex + 1} rolled a {_diceResult}"; }
    }

    private void SetPhysicalText(int diceResult)
    {
        if (resultTextBoxes.Count == 0 && resultContainer != null && textPrefab != null)
        {
            resultTextBoxes.Add(Instantiate(textPrefab, resultContainer.transform));
        }

        if (resultTextBoxes.Count > 0)
        {
            TMP_Text text = resultTextBoxes[0].GetComponent<TMP_Text>();
            if (text != null) { text.text = $"Dice rolled a {diceResult}"; }
        }
    }
}