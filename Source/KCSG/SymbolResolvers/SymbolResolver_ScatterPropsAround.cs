﻿using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.BaseGen;
using Verse;
using static KCSG.SettlementGenUtils;

namespace KCSG
{
    internal class SymbolResolver_ScatterPropsAround : SymbolResolver
    {
        private List<IntVec3> usedSpots = new List<IntVec3>();

        public override void Resolve(ResolveParams rp)
        {
            Map map = BaseGen.globalSettings.map;
            var propsOpt = GenOption.PropsOptions;

            if (propsOpt.scatterProps && propsOpt.scatterPropsDefs.Count > 0)
            {
                usedSpots = new List<IntVec3>();

                var propsStart = DateTime.Now;
                for (int i = 0; i < propsOpt.scatterMaxAmount; i++)
                {
                    var prop = propsOpt.scatterPropsDefs.RandomElement();
                    Rot4 rot = Rot4.North;

                    if (prop.rotatable && Rand.Bool)
                        rot = Rot4.East;

                    if (RCellFinder.TryFindRandomCellNearTheCenterOfTheMapWith(c =>
                    {
                        if (!rp.rect.Contains(c))
                            return false;

                        if (GenUtils.NearUsedSpot(usedSpots, c, propsOpt.scatterMinDistance))
                            return false;

                        CellRect rect;
                        if (rot == Rot4.North)
                            rect = new CellRect(c.x, c.z, prop.size.x, prop.size.z);
                        else
                            rect = new CellRect(c.x, c.z, prop.size.z, prop.size.x);

                        foreach (var ce in rect)
                        {
                            if (grid[ce.z][ce.x] == CellType.Used || !ce.Walkable(map))
                                return false;
                        }

                        return true;
                    }, map, out IntVec3 cell))
                    {
                        Thing thing = ThingMaker.MakeThing(prop, GenStuff.DefaultStuffFor(prop));
                        thing.SetFactionDirect(map.ParentFaction);

                        if (prop.rotatable)
                            GenSpawn.Spawn(thing, cell, map, rot);
                        else
                            GenSpawn.Spawn(thing, cell, map);

                        usedSpots.Add(cell);
                    }
                }
                Debug.Message($"Props spawning time: {(DateTime.Now - propsStart).TotalMilliseconds}ms.");
            }

            Debug.Message($"Total time (without pawn gen): {(DateTime.Now - startTime).TotalSeconds}s.");
        }
    }
}