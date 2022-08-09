﻿using Gellybeans.ECS;
using System.Data;

namespace Gellybeans.Pathfinder
{
    public class StatBlock
    {

        public DataTable Table { get; set; } = new DataTable();
        
        public Dictionary<string, string>   Info        { get; private set; } = new Dictionary<string, string>();
        
        public List<Item>                   Inventory   { get; set; } = new List<Item>();
        public List<StatModifier>           StatMods    { get; set; } = new List<StatModifier>();

        public List<Attack>                 Attacks     { get; set; } = new List<Attack>();

        
        
        public int this[string statName]
        {
            get { return (int)Table.Rows[0][statName]; }
            set { Table.Rows[0][statName] = value; }
        }



        public void AddStat(string varName, Stat stat)
        {
            Table.Columns.Add
                (
                    new DataColumn()
                    {
                        AllowDBNull     = false,
                        ColumnName      = varName,
                        DataType        = typeof(Stat),
                        DefaultValue    = stat.Value
                    }
                );
        }

        public void AddFormula(string exprName, string expression)
        {
            Table.Columns.Add
                (
                    new DataColumn()
                    {
                        AllowDBNull     = false,
                        ColumnName      = exprName,
                        DataType        = typeof(int),
                        DefaultValue    = 0,
                        Expression      = expression
                    }
                );
        }


        public static StatBlock DefaultPathfinder()
        {
            var statBlock = new StatBlock()
            {
              
                Info = new Dictionary<string, string>()
                {
                    ["NAME"] = "NAME ME",
                    ["LEVELS"] = "",
                    ["DEITY"] = "",
                    ["HOME"] = "",
                    ["GENDER"] = "",
                    ["HAIR"] = "",
                    ["EYES"] = "",
                    ["BIO"] = ""
                },               
            };

            var stats = new Dictionary<string, Stat>()
            {
                ["SIZE"] = 0,
                ["ALIGN"] = 0,
                ["AGE"] = 0,
                ["HEIGHT"] = 100,
                ["WEIGHT"] = 100,

                ["HP_BASE"] = 0,
                ["HP_TEMP"] = 0,
                ["HP_DAMAGE"] = 0,
                ["HP_NONLETHAL"] = 0,

                ["STR"] = 10,
                ["DEX"] = 10,
                ["CON"] = 10,
                ["INT"] = 10,
                ["WIS"] = 10,
                ["CHA"] = 10,

                //since damage and temporary bonuses apply symmetrical effects, the same field can be used for both. neat. :)
                ["STR_TEMP"] = 0,
                ["DEX_TEMP"] = 0,
                ["CON_TEMP"] = 0,
                ["INT_TEMP"] = 0,
                ["WIS_TEMP"] = 0,
                ["CHA_TEMP"] = 0,

                ["MOVE"] = 0,

                ["INIT"] = 0,

                ["AC"] = 10,
                ["AC_TOUCH"] = 10,
                ["AC_FLAT"] = 10,

                ["AC_MAXDEX"] = 99,
                ["CHECK_PENALTY"] = 0,

                ["DR_TYPE"] = 0,  //bitmask                  
                ["DR_VALUE"] = 0,

                ["SR"] = 0,

                ["SAVE_FORT"] = 0,
                ["SAVE_REFLEX"] = 0,
                ["SAVE_WILL"] = 0,

                ["BAB"] = 0,

                ["CMD"] = 10,
                ["CMB"] = 0,

                ["ARCANE_FAIL"] = 0,

                //skills
                ["ACROBATICS"] = 0,
                ["APPRAISE"] = 0,
                ["BLUFF"] = 0,
                ["CLIMB"] = 0,
                ["DIPLOMACY"] = 0,
                ["DISABLEDEVICE"] = 0,
                ["DISGUISE"] = 0,
                ["ESCAPE"] = 0,
                ["FLY"] = 0,
                ["HANDLEANIMAL"] = 0,
                ["HEAL"] = 0,
                ["INTIMIDATE"] = 0,
                ["ARCANA"] = 0,
                ["DUNGEONEERING"] = 0,
                ["GEOGRAPHY"] = 0,
                ["HISTORY"] = 0,
                ["LOCAL"] = 0,
                ["NATURE"] = 0,
                ["NOBILITY"] = 0,
                ["PLANES"] = 0,
                ["RELIGION"] = 0,
                ["LINGUISTICS"] = 0,
                ["PERCEPTION"] = 0,
                ["RIDE"] = 0,
                ["SENSEMOTIVE"] = 0,
                ["SLEIGHTOFHAND"] = 0,
                ["SPELLCRAFT"] = 0,
                ["STEALTH"] = 0,
                ["SURVIVAL"] = 0,
                ["SWIM"] = 0,
                ["USEMAGICDEVICE"] = 0,
            };


            foreach(var stat in stats)
            {
                statBlock.Table.Columns.Add
                    (
                        new DataColumn()
                        {
                            AllowDBNull =       false,
                            ColumnName =        stat.Key,
                            DataType =          typeof(Stat),
                            DefaultValue =      stat.Value,                            
                        }
                    );
            }

            statBlock.AddFormula("STR_MOD", "(STR - 10) / 2");            

            return statBlock;
        }

    }
}