using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyGiantStudio.BetterInspector
{
    public class Scales : ScriptableObject
    {
        public int selectedUnit;
        public List<Unit> units = new List<Unit>();

        public float CurrentUnitValue() => UnitValue(selectedUnit);

        public float UnitValue(int unit)
        {
            if (unit < 0 || unit > units.Count) return 0; //for invalid value

            return units[unit].value;
        }

        public string[] GetAvailableUnits()
        {
            string[] availableUnits = new string[units.Count];
            for (int i = 0; i < availableUnits.Length; i++)
            {
                availableUnits[i] = units[i].name;
            }
            return availableUnits;
        }

        [ContextMenu("Reset")]
        public void Reset()
        {
            selectedUnit = 0;
            units = new List<Unit>
            {
                new("Meter", 1),
                new("Kilometer", 0.001f),
                new("Centimeter", 100),
                new("Millimeter", 1000),
                new("Feet", 3.28084f),
                new("Inch", 39.3701f),
                new("Yards", 1.09f),
                new("Miles", 0.00062f),
                new("NauticalMile", 0.000539957f),
                new("Banana", 5.618f)
            };

#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }
    }

    [System.Serializable]
    public class Unit
    {
        public string name;

        /// <summary>
        /// This is the value of the unit compared to meter, the default unity scale
        /// </summary>
        public float value;

        public Unit(string name, float value)
        {
            this.name = name;
            this.value = value;
        }
    }
}