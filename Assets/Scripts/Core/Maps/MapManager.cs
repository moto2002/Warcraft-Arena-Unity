﻿using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Core
{
    public class MapManager
    {
        private readonly Dictionary<int, Map> baseMaps = new Dictionary<int, Map>();
        private readonly Mutex mapsLock = new Mutex(true);
        private readonly MapUpdater mapUpdater = new MapUpdater();

        private WorldManager worldManager;

        internal MapManager(WorldManager worldManager)
        {
            this.worldManager = worldManager;

            if (!worldManager.HasClientLogic)
                mapUpdater.Activate(8);
        }

        internal void Dispose()
        {
            foreach (var mapEntry in baseMaps)
                mapEntry.Value.Deinitialize();
            baseMaps.Clear();

            if (!worldManager.HasClientLogic)
                mapUpdater.Deactivate();

            worldManager = null;
        }

        internal void DoUpdate(int timeDiff)
        {
            foreach (var map in baseMaps)
            {
                if (mapUpdater.Activated)
                    mapUpdater.ScheduleUpdate(map.Value, timeDiff);
                else
                    map.Value.DoUpdate(timeDiff);
            }
        
            if (mapUpdater.Activated)
                mapUpdater.Wait();

            foreach (var mapEntry in baseMaps)
                mapEntry.Value.DelayedUpdate(timeDiff);
        }

        internal void InitializeLoadedMap(int mapId)
        {
            Map map = baseMaps.LookupEntry(mapId);

            if (map == null)
            {
                mapsLock.WaitOne();

                map = new Map(mapId);
                baseMaps[mapId] = map;
                map.Initialize(worldManager, SceneManager.GetActiveScene());

                mapsLock.ReleaseMutex();
            }

            Assert.IsNotNull(map);
        }

        internal void DoForAllMaps(Action<Map> mapAction)
        {
            mapsLock.WaitOne();

            foreach (var mapEntry in baseMaps)
                mapAction(mapEntry.Value);
           
            mapsLock.ReleaseMutex();
        }

        internal void DoForAllMapsWithMapId(int mapId, Action<Map> mapAction)
        {
            mapsLock.WaitOne();

            var map = baseMaps.LookupEntry(mapId);
            if (map != null)
            {
                mapAction(map);
            }

            mapsLock.ReleaseMutex();
        }

        public Map FindMap(int mapId)
        {
            return baseMaps.LookupEntry(mapId);
        }
    }
}