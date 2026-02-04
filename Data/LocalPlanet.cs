using System;
using System.Threading;
using static PlanetWormhole.Constants;
using static PlanetWormhole.Util.Functions;

namespace PlanetWormhole.Data
{
    internal class LocalPlanet
    {
        public int[] produced;
        public int[] served;
        public int[] buffer;
        public int inc;
        public int consumedProliferator;
        public int sumSpray;
        public bool spray;
        public PlanetFactory factory;
        public Cosmic cosmic;
        public AutoResetEvent completeSignal;
        private uint r;

        private static ThreadLocal<Random> rng = new ThreadLocal<Random>(() => new Random());

        public LocalPlanet()
        {
            produced = new int[MAX_ITEM_COUNT];
            served = new int[MAX_ITEM_COUNT];
            buffer = new int[MAX_ITEM_COUNT];
            Init();
            completeSignal = new AutoResetEvent(false);
        }

        public void Init()
        {
            Array.Clear(buffer, 0, MAX_ITEM_COUNT);
            inc = 0;
        }

        public void SetFactory(PlanetFactory factory)
        {
            this.factory = factory;
        }

        public void SetCosmic(Cosmic cosmic)
        {
            this.cosmic = cosmic;
        }
        public void PatchPlanet(object stateInfo = null)
        {
            Reset();
            RegisterTrash();
            RegisterPowerSystem();
            RegisterTurret();
            RegisterMiner();
            RegisterAssembler();
            RegisterFractionator();
            RegisterLab();
            RegisterEjector();
            RegisterSilo();
            RegisterStorage();
            RegisterStation();
            Spray();
            ConsumeBuffer();
            ConsumeTrash();
            ConsumeStorage();
            ConsumePowerSystem();
            ConsumeTurret();
            ConsumeMiner();
            ConsumeFractionator();
            ConsumeAssembler();
            ConsumeLab();
            ConsumeEjector();
            ConsumeSilo();
            ConsumeStation();
            completeSignal.Set();
        }

        private void Reset()
        {
            Array.Clear(produced, 0, MAX_ITEM_COUNT);
            Array.Clear(served, 0, MAX_ITEM_COUNT);
            spray = true;
            sumSpray = 0;
            consumedProliferator = 0;
            r = (uint) rng.Value.Next();
        }

        private void RegisterAssembler()
        {
            AssemblerComponent[] pool = factory.factorySystem.assemblerPool;

            for (int i = 1; i < factory.factorySystem.assemblerCursor; i++)
            {
                if (pool[i].id == i && pool[i].recipeId > 0)
                {
                    ref var ptr = ref pool[i];
                    for (int j = 0; j < ptr.produced.Length; j++)
                    {
                        if (ptr.produced[j] > 0)
                        {
                            produced[ptr.recipeExecuteData.products[j]] += ptr.produced[j];
                        }
                    }
                    for (int j = 0; j < ptr.recipeExecuteData.requireCounts.Length; j++)
                    {
                        if (ptr.needs[j] > 0)
                        {
                            int count = _positive(3 * ptr.recipeExecuteData.requireCounts[j] - ptr.served[j]);
                            sumSpray += count;
                            served[ptr.needs[j]] += count;
                        }
                    }
                }
            }
        }

        private void ConsumeAssembler()
        {
            AssemblerComponent[] pool = factory.factorySystem.assemblerPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.assemblerCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.assemblerCursor - 1)) + 1;
                if (pool[i].id == i && pool[i].recipeId > 0)
                {
                    ref var ptr = ref pool[i];
                    for (int j = 0; j < ptr.produced.Length; j++)
                    {
                        if (ptr.produced[j] > 0)
                        {
                            int itemId = ptr.recipeExecuteData.products[j];
                            _produce(itemId, served, ref ptr.produced[j], ref count);
                        }
                    }
                    if (ptr.produced.Length > 1)
                    {
                        int len = ptr.produced.Length;
                        switch (ptr.recipeType)
                        {
                            case ERecipeType.Smelt:
                                for (int j = 0; j < len; j++)
                                {
                                    if (ptr.produced[j] + ptr.recipeExecuteData.productCounts[j] > 100
                                        && buffer[ptr.recipeExecuteData.products[j]] < BUFFER_SIZE)
                                    {
                                        ptr.produced[j] -= ptr.recipeExecuteData.productCounts[j];
                                        buffer[ptr.recipeExecuteData.products[j]] += ptr.recipeExecuteData.productCounts[j];
                                    }
                                }
                                break;
                            case ERecipeType.Assemble:
                                for (int j = 0; j < len; j++)
                                {
                                    if (ptr.produced[j] > ptr.recipeExecuteData.productCounts[j] * 9
                                        && buffer[ptr.recipeExecuteData.products[j]] < BUFFER_SIZE)
                                    {
                                        ptr.produced[j] -= ptr.recipeExecuteData.productCounts[j];
                                        buffer[ptr.recipeExecuteData.products[j]] += ptr.recipeExecuteData.productCounts[j];
                                    }
                                }
                                break;
                            default:
                                for (int j = 0; j < len; j++)
                                {
                                    if (ptr.produced[j] > ptr.recipeExecuteData.productCounts[j] * 19
                                        && buffer[ptr.recipeExecuteData.products[j]] < BUFFER_SIZE)
                                    {
                                        ptr.produced[j] -= ptr.recipeExecuteData.productCounts[j];
                                        buffer[ptr.recipeExecuteData.products[j]] += ptr.recipeExecuteData.productCounts[j];
                                    }
                                }
                                break;
                        }
                    }
                    for (int j = 0; j < ptr.recipeExecuteData.requireCounts.Length; j++)
                    {
                        if (ptr.needs[j] > 0)
                        {
                            int itemId = ptr.needs[j];
                            _serve(itemId, produced, ref ptr.served[j], 3 * ptr.recipeExecuteData.requireCounts[j], ref count);
                            if (spray)
                            {
                                inc -= count * INC_ABILITY;
                                ptr.incServed[j] += INC_ABILITY * count;
                            }
                        }
                    }
                }
            }
        }

        private void RegisterStation()
        {
            StationComponent[] pool = factory.transport.stationPool;
            for (int i = 1; i < factory.transport.stationCursor; i++)
            {
                var ptr = pool[i];
                if (ptr != null && ptr.id == i && ptr.storage != null)
                {                    
                    StationStore[] storage = pool[i].storage;
                    for (int j = 0; j < storage.Length; j++)
                    {
                        if (storage[j].itemId > 0)
                        {
                            if (storage[j].localLogic == ELogisticStorage.Supply)
                            {
                                produced[storage[j].itemId] += storage[j].count;
                            }
                            else if (storage[j].localLogic == ELogisticStorage.Demand)
                            {
                                int count = _positive(storage[j].max - storage[j].count);
                                served[storage[j].itemId] += count;
                            }
                        }
                    }
                    if (ptr.needs[5] == WARPER && factory.gameData.history.TechUnlocked(SHIP_ENGINE_4))
                    {
                        served[WARPER] += _positive(ptr.warperMaxCount - ptr.warperCount);
                    }
                }
            }
        }

        private void ConsumeStation()
        {
            StationComponent[] pool = factory.transport.stationPool;
            int count = 0;
            for (int k = 1; k < factory.transport.stationCursor; k++)
            {
                int i = (int)((r + k) % (factory.transport.stationCursor - 1)) + 1;
                var ptr = pool[i];
                if (ptr != null && ptr.id == i && ptr.storage != null)
                {
                    if (ptr.needs[5] == WARPER && factory.gameData.history.TechUnlocked(SHIP_ENGINE_4))
                    {
                        _serve(WARPER, produced, ref ptr.warperCount, ptr.warperMaxCount, ref count);
                    }
                }
            }
            for (int k = 1; k < factory.transport.stationCursor; k++)
            {
                int i = (int)((r + k) % (factory.transport.stationCursor - 1)) + 1;
                var ptr = pool[i];
                if (ptr != null && ptr.id == i && ptr.storage != null)
                {
                    StationStore[] storage = ptr.storage;
                    for (int j = 0; j < storage.Length; j++)
                    {
                        if (storage[j].itemId > 0)
                        {
                            int itemId = storage[j].itemId;
                            if (storage[j].localLogic == ELogisticStorage.Supply)
                            {
                                _produce(itemId, served, ref storage[j].count, ref count);
                                int incAdd = _split_inc(storage[j].inc, count);
                                storage[j].inc -= incAdd;
                                inc += incAdd;
                            } else if (storage[j].localLogic == ELogisticStorage.Demand)
                            {
                                _serve(itemId, produced, ref storage[j].count, storage[j].max, ref count);
                            }
                        }
                    }
                }
            }
        }

        private void RegisterPowerSystem()
        {
            PowerGeneratorComponent[] pool = factory.powerSystem.genPool;
            for (int i = 1; i < factory.powerSystem.genCursor; i++)
            {
                if (pool[i].id == i)                
                {
                    ref var ptr = ref pool[i];
                    if (ptr.catalystId > 0 && factory.gameData.history.TechUnlocked(IONOSPHERIC_TECH))
                    {
                        int count = _positive((72000 - ptr.catalystPoint) / 3600);
                        sumSpray += count;
                        served[ptr.catalystId] += count;
                        if (ptr.productId > 0)
                        {
                            produced[ptr.productId] += (int) ptr.productCount;
                        }
                    }
                    int[] fuelNeeds = ItemProto.fuelNeeds[ptr.fuelMask];
                    if (ptr.fuelId > 0 || (fuelNeeds != null && fuelNeeds.Length > 0))
                    {
                        int itemId;
                        if (ptr.fuelId > 0)
                        {
                            itemId = ptr.fuelId;
                        }
                        else if (ptr.curFuelId > 0)
                        {
                            itemId = ptr.curFuelId;
                        }
                        else if (fuelNeeds.Length > 0)
                        {
                            itemId = fuelNeeds[0];
                            ptr.SetNewFuel(itemId, 0, 0);
                        }
                        else
                        {
                            continue;
                        }
                        int count = _positive(10 - ptr.fuelCount);
                        sumSpray += count;
                        served[itemId] += count;
                    }
                }
            }
            PowerExchangerComponent[] excPool = factory.powerSystem.excPool;
            for (int i = 1; i < factory.powerSystem.excCursor; i++)
            {
                ref var ptr = ref excPool[i];
                if (ptr.id == i && ptr.fullId > 0 && ptr.emptyId > 0)
                {
                    if (_float_equal(ptr.targetState, 1.0f))
                    {
                        produced[ptr.fullId] += ptr.fullCount;
                        served[ptr.emptyId] += _positive(PowerExchangerComponent.maxCount - ptr.emptyCount);
                    } else if (_float_equal(ptr.targetState, -1.0f))
                    {
                        produced[ptr.emptyId] += ptr.emptyCount;
                        served[ptr.fullId] += _positive(PowerExchangerComponent.maxCount - ptr.fullCount);
                    }
                }
            }
        }

        private void ConsumePowerSystem()
        {
            PowerGeneratorComponent[] pool = factory.powerSystem.genPool;
            int count = 0;
            for (int k = 1; k < factory.powerSystem.genCursor; k++)
            {
                int i = (int)((r + k) % (factory.powerSystem.genCursor - 1)) + 1;
                ref var ptr = ref pool[i];
                if (ptr.id == i)
                {                    
                    if (ptr.catalystId > 0 && factory.gameData.history.TechUnlocked(IONOSPHERIC_TECH))
                    {
                        int itemId = ptr.catalystId;
                        if (produced[itemId] > 0)
                        {
                            count = _positive(Math.Min((72000 - ptr.catalystPoint) / 3600, produced[itemId]));
                            produced[itemId] -= count;
                            ptr.catalystPoint += count * 3600;
                            if (spray)
                            {
                                inc -= count * INC_ABILITY;
                                ptr.catalystIncPoint += count * 3600 * INC_ABILITY;
                            }
                        }
                        if (ptr.productId > 0)
                        {
                            itemId = ptr.productId;
                            if (served[itemId] > 0)
                            {
                                count = Math.Min((int)ptr.productCount, served[itemId]);
                                served[itemId] -= count;
                                ptr.productCount -= count;
                            }
                        }
                    }
                    if (ptr.fuelId > 0)
                    {
                        int itemId = ptr.fuelId;
                        if (produced[itemId] > 0)
                        {
                            count = _positive(Math.Min(10 - ptr.fuelCount, produced[itemId]));
                            produced[itemId] -= count;
                            ptr.fuelCount += (short) count;
                            if (spray)
                            {
                                inc -= count * INC_ABILITY;
                                ptr.fuelInc += (short) (INC_ABILITY * count);
                            }
                        }
                    }
                }
            }
            PowerExchangerComponent[] excPool = factory.powerSystem.excPool;
            for (int k = 1; k < factory.powerSystem.excCursor; k++)
            {
                int i = (int)((r + k) % (factory.powerSystem.excCursor - 1)) + 1;
                ref var ptr = ref excPool[i];
                if (ptr.id == i && ptr.fullId > 0 && ptr.emptyId > 0)
                {
                    int fullIndex = ptr.fullId;
                    int emptyIndex = ptr.emptyId;
                    if (_float_equal(ptr.targetState, 1.0f))
                    {
                        if (served[fullIndex] > 0)
                        {
                            count = Math.Min(ptr.fullCount, served[fullIndex]);
                            served[fullIndex] -= count;
                            ptr.fullCount -= (short) count;
                        }
                        if (produced[emptyIndex] > 0)
                        {
                            count = _positive(Math.Min(PowerExchangerComponent.maxCount - ptr.emptyCount, produced[emptyIndex]));
                            produced[emptyIndex] -= count;
                            ptr.emptyCount += (short) count;
                        }
                    }
                    else if (_float_equal(ptr.targetState, -1.0f))
                    {
                        if (served[emptyIndex] > 0)
                        {
                            count = Math.Min(ptr.emptyCount, served[emptyIndex]);
                            served[emptyIndex] -= count;
                            ptr.emptyCount -= (short)count;
                        }
                        if (produced[fullIndex] > 0)
                        {
                            count = _positive(Math.Min(PowerExchangerComponent.maxCount - ptr.fullCount, produced[fullIndex]));
                            produced[fullIndex] -= count;
                            ptr.fullCount += (short)count;
                        }
                    }
                }
            }
        }

        private void RegisterMiner()
        {
            MinerComponent[] pool = factory.factorySystem.minerPool;
            for (int i = 1; i < factory.factorySystem.minerCursor; i++)
            {
                if (pool[i].id == i)
                {
                    ref var ptr = ref pool[i];
                    if (ptr.productId > 0)
                    {
                        produced[ptr.productId] += ptr.productCount;
                    }
                }
            }
        }

        private void ConsumeMiner()
        {
            MinerComponent[] pool = factory.factorySystem.minerPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.minerCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.minerCursor - 1)) + 1;
                if (pool[i].id == i)
                {
                    ref var ptr = ref pool[i];
                    if (ptr.productId > 0)
                    {
                        int itemId = ptr.productId;
                        _produce(itemId, served, ref ptr.productCount, ref count);
                    }
                }
            }
        }

        private void RegisterLab()
        {
            LabComponent[] pool = factory.factorySystem.labPool;
            for (int i = 1; i < factory.factorySystem.labCursor; i++)
            {
                ref var ptr = ref pool[i];
                if (ptr.id == i && !ptr.researchMode && ptr.recipeId > 0)
                {
                    if (ptr.recipeExecuteData.productCounts != null && ptr.recipeExecuteData.productCounts.Length > 0)
                    {
                        for (int j = 0; j < ptr.recipeExecuteData.productCounts.Length; j++)
                        {
                            produced[ptr.recipeExecuteData.products[j]] += ptr.produced[j];
                        }
                    }
                    for (int j = 0; j < ptr.needs.Length; j++)
                    {
                        if (ptr.needs[j] > 0)
                        {
                            int count = _positive(4 - ptr.served[j]);
                            sumSpray += count;
                            served[ptr.needs[j]] += count;
                        }
                    }
                }
                if (ptr.id == i && ptr.researchMode)
                {
                    for (int j = 0; j < ptr.needs.Length; j++)
                    {
                        if (ptr.needs[j] > 0)
                        {
                            int count = _positive((36000 - ptr.matrixServed[j]) / 3600);
                            sumSpray += count;
                            served[ptr.needs[j]] += count;
                        }
                    }
                }
            }
        }
        

        private void ConsumeLab()
        {
            LabComponent[] pool = factory.factorySystem.labPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.labCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.labCursor - 1)) + 1;
                ref var ptr = ref pool[i];
                if (ptr.id == i && !ptr.researchMode && ptr.recipeId > 0)
                {
                    if (ptr.recipeExecuteData.productCounts != null && ptr.recipeExecuteData.productCounts.Length > 0)
                    {
                        for (int j = 0; j < ptr.recipeExecuteData.productCounts.Length; j++)
                        {
                            int itemId = ptr.recipeExecuteData.products[j];
                            _produce(itemId, served, ref ptr.produced[j], ref count);
                        }
                    }
                    for (int j = 0; j < ptr.needs.Length; j++)
                    {
                        if (ptr.needs[j] > 0)
                        {
                            int itemId = ptr.needs[j];
                            _serve(itemId, produced, ref ptr.served[j], 4, ref count);
                            if (spray)
                            {
                                inc -= count * INC_ABILITY;
                                ptr.incServed[j] += INC_ABILITY * count;
                            }
                        }
                    }
                }
                if (ptr.id == i && ptr.researchMode)
                {
                    for (int j = 0; j < ptr.needs.Length; j++)
                    {
                        if (ptr.needs[j] > 0)
                        {
                            int itemId = ptr.needs[j];
                            if (produced[itemId] > 0)
                            {
                                count = _positive(Math.Min((36000 - ptr.matrixServed[j]) / 3600, produced[itemId]));
                                produced[itemId] -= count;
                                ptr.matrixServed[j] += count * 3600;
                                if (spray)
                                {
                                    inc -= count * INC_ABILITY ;
                                    ptr.matrixIncServed[j] += INC_ABILITY * count * 3600;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RegisterEjector()
        {
            EjectorComponent[] pool = factory.factorySystem.ejectorPool;
            for (int i = 1; i < factory.factorySystem.ejectorCursor; i++)
            {
                ref var ptr = ref pool[i];
                if (ptr.id == i && ptr.bulletId > 0)
                {
                    int count = _positive(20 - ptr.bulletCount);
                    sumSpray += count;
                    served[ptr.bulletId] += count;
                }
            }
        }

        private void ConsumeEjector()
        {
            EjectorComponent[] pool = factory.factorySystem.ejectorPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.ejectorCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.ejectorCursor - 1)) + 1;
                ref var ptr = ref pool[i];
                if (ptr.id == i && ptr.bulletId > 0)
                {
                    int itemId = ptr.bulletId;
                    _serve(itemId, produced, ref ptr.bulletCount, 20, ref count);
                    if (spray)
                    {
                        inc -= count * INC_ABILITY;
                        ptr.bulletInc += INC_ABILITY * count;
                    }
                }
            }
        }

        private void RegisterSilo()
        {
            SiloComponent[] pool = factory.factorySystem.siloPool;
            for (int i = 1; i < factory.factorySystem.siloCursor; i++)
            {
                ref var ptr = ref pool[i];
                if (ptr.id == i && ptr.bulletId > 0)
                {
                    int count = _positive(20 - ptr.bulletCount);
                    sumSpray += count;
                    served[ptr.bulletId] += count;
                }
            }
        }

        private void ConsumeSilo()
        {
            SiloComponent[] pool = factory.factorySystem.siloPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.siloCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.siloCursor - 1)) + 1;
                ref var ptr = ref pool[i];
                if (ptr.id == i && ptr.bulletId > 0)
                {
                    int itemId = ptr.bulletId;
                    _serve(itemId, produced, ref ptr.bulletCount, 20, ref count);
                    if (spray)
                    {
                        inc -= count * INC_ABILITY;
                        ptr.bulletInc += INC_ABILITY * count;
                    }
                }
            }
        }

        private void RegisterStorage()
        {
            StorageComponent[] storagePool = factory.factoryStorage.storagePool;
            for (int i = 1; i < factory.factoryStorage.storageCursor; i++)
            {
                if (storagePool[i] != null && storagePool[i].id == i)
                {
                    if (storagePool[i].grids == null)
                    {
                        continue;
                    }
                    for(int j = 0; j < storagePool[i].grids.Length; j++)
                    {
                        if (storagePool[i].grids[j].itemId > 0)
                        {
                            produced[storagePool[i].grids[j].itemId] += storagePool[i].grids[j].count;
                        }
                    }
                }
            }
            TankComponent[] tankPool = factory.factoryStorage.tankPool;
            for (int i = 1; i < factory.factoryStorage.tankCursor; i++)
            {
                if (tankPool[i].id == i)
                {
                    if (tankPool[i].fluidId > 0)
                    {
                        produced[tankPool[i].fluidId] += tankPool[i].fluidCount;
                    }
                }
            }
        }

        private void ConsumeStorage()
        {
            StorageComponent[] storagePool = factory.factoryStorage.storagePool;
            int count = 0;
            for (int k = 1; k < factory.factoryStorage.storageCursor; k++)
            {
                int i = (int)((r + k) % (factory.factoryStorage.storageCursor - 1)) + 1;
                if (storagePool[i] != null && storagePool[i].id == i)
                {
                    if (storagePool[i].grids == null)
                    {
                        continue;
                    }
                    bool change = false;
                    for (int j = 0; j < storagePool[i].grids.Length; j++)
                    {
                        if (storagePool[i].grids[j].itemId > 0)
                        {
                            int itemId = storagePool[i].grids[j].itemId;
                            _produce(itemId, served, ref storagePool[i].grids[j].count, ref count);
                            int incAdd = _split_inc(storagePool[i].grids[j].inc, count);
                            inc += incAdd;
                            storagePool[i].grids[j].inc -= incAdd;
                            if (storagePool[i].grids[j].count <= 0)
                            {
                                storagePool[i].grids[j].itemId = 0;
                                storagePool[i].grids[j].count = 0;
                                storagePool[i].grids[j].inc = 0;
                            }
                            if (count > 0)
                            {
                                change = true;
                            }
                        }
                    }
                    if (change)
                    {
                        storagePool[i].Sort();
                    }
                }
            }
            TankComponent[] tankPool = factory.factoryStorage.tankPool;
            for (int k = 1; k < factory.factoryStorage.tankCursor; k++)
            {
                int i = (int)((r + k) % (factory.factoryStorage.tankCursor - 1)) + 1;
                if (tankPool[i].id == i)
                {
                    if (tankPool[i].fluidId > 0)
                    {
                        int itemId = tankPool[i].fluidId;
                        if (buffer[itemId] > 0)
                        {
                            if (tankPool[i].fluidCount < tankPool[i].fluidCapacity)
                            {
                                count = Math.Min(buffer[itemId], tankPool[i].fluidCapacity - tankPool[i].fluidCount);
                                buffer[itemId] -= count;
                                tankPool[i].fluidCount += count;
                            } else if (tankPool[i].nextTankId > 0)
                            {
                                tankPool[tankPool[i].nextTankId].fluidId = itemId;
                            }
                        }
                        _produce(itemId, served, ref tankPool[i].fluidCount, ref count);
                        int incAdd = _split_inc(tankPool[i].fluidInc, count);
                        inc += incAdd;
                        tankPool[i].fluidInc -= incAdd;
                        if (tankPool[i].fluidCount <= 0)
                        {
                            tankPool[i].fluidId = 0;
                            tankPool[i].fluidCount = 0;
                            tankPool[i].fluidInc = 0;
                        }
                    }
                }
            }
        }

        private void RegisterTrash()
        {
            Cosmic.mutex.WaitOne();
            for (int i = 0; i < MAX_ITEM_COUNT; i++)
            {
                    produced[i] += cosmic.trashProduced[i];
            }
            Cosmic.mutex.ReleaseMutex();
        }

        private void ConsumeTrash()
        {
            int count = 0;
            Cosmic.mutex.WaitOne();
            for (int i = 0; i < MAX_ITEM_COUNT; i++)
            {
                if (served[i] > 0)
                {
                    count = _positive(Math.Min(cosmic.trashProduced[i] - cosmic.trashServed[i], served[i]));
                    served[i] -= count;
                    cosmic.trashServed[i] += count;
                }
            }
            Cosmic.mutex.ReleaseMutex();
        }

        private void ConsumeBuffer()
        {
            for (int i = 0; i < MAX_ITEM_COUNT; i++)
            {
                if (served[i] > 0 && buffer[i] > 0)
                {
                    int count = Math.Min(buffer[i], served[i]);
                    served[i] -= count;
                    buffer[i] -= count;
                }
            }
        }

        private void RegisterFractionator()
        {
            FractionatorComponent[] pool = factory.factorySystem.fractionatorPool;
            for(int i = 1; i < factory.factorySystem.fractionatorCursor; i++)
            {
                if (pool[i].id == i)
                {
                    if (pool[i].fluidId > 0)
                    {
                        int count = _positive(pool[i].fluidInputMax * 4 - pool[i].fluidInputCount);
                        served[pool[i].fluidId] += count;
                        sumSpray += count;
                        count = _positive(pool[i].fluidOutputCount);
                        produced[pool[i].fluidId] += count;
                    }
                    if (pool[i].productId > 0)
                    {
                        produced[pool[i].productId] += _positive(pool[i].productOutputCount - 1);
                    }
                }
            }
        }

        private void ConsumeFractionator()
        {
            FractionatorComponent[] pool = factory.factorySystem.fractionatorPool;
            int count = 0;
            for (int k = 1; k < factory.factorySystem.fractionatorCursor; k++)
            {
                int i = (int)((r + k) % (factory.factorySystem.fractionatorCursor - 1)) + 1;
                ref var ptr = ref pool[i];
                if (ptr.id == i)
                {
                    if (ptr.fluidId > 0)
                    {
                        int itemId = ptr.fluidId;
                        _serve(itemId, produced, ref ptr.fluidInputCount, 4 * ptr.fluidInputMax, ref count);
                        ptr.fluidInputCargoCount += .25f * count;
                        if (spray)
                        {
                            inc -= count * INC_ABILITY;
                            ptr.fluidInputInc += count * INC_ABILITY;
                        }
                        if (buffer[itemId] < BUFFER_SIZE && ptr.fluidOutputCount > ptr.fluidOutputMax / 2)
                        {
                            buffer[itemId] += ptr.fluidOutputCount;
                            ptr.fluidOutputCount = 0;
                        } else
                        {
                            _produce(itemId, served, ref ptr.fluidOutputCount, ref count);
                        }
                        int incAdd = _split_inc(ptr.fluidOutputInc, count);
                        inc += incAdd;
                        ptr.fluidOutputInc -= incAdd;
                    }
                    if (ptr.productId > 1)
                    {
                        int itemId = ptr.productId;
                        ptr.productOutputCount -= 1;
                        _produce(itemId, served, ref ptr.productOutputCount, ref count);
                        ptr.productOutputCount += 1;
                    }
                }
            }
        }

        private void RegisterTurret()
        {
            TurretComponent[] pool = factory.defenseSystem.turrets.buffer;
            int count = 0;
            for (int k = 1; k < factory.defenseSystem.turrets.cursor; k++)
            {
                ref var ptr = ref pool[k];
                if (ptr.id == k)
                {
                    if (ptr.itemId > 0)
                    {
                        int itemId = ptr.itemId;
                        count = _positive(5 - ptr.itemCount);
                        served[itemId] += count;
                        sumSpray += count;
                    }
                }
            }
        }

        private void ConsumeTurret()
        {
            TurretComponent[] pool = factory.defenseSystem.turrets.buffer;
            int count = 0;
            for (int k = 1; k < factory.defenseSystem.turrets.cursor; k++)
            {
                int i = (int)((r + k) % (factory.defenseSystem.turrets.cursor - 1)) + 1;
                ref var ptr = ref pool[i];
                if (ptr.id == i)
                {
                    if (ptr.itemId > 0)
                    {
                        int itemId = ptr.itemId;
                        int itemCount = ptr.itemCount;
                        _serve(itemId, produced, ref itemCount, 5, ref count);
                        ptr.itemCount = (short)itemCount;
                        if (spray)
                        {
                            inc -= count * INC_ABILITY;
                            ptr.itemInc += (short)(count * INC_ABILITY);
                        }
                    }
                }
            }
        }

        private void Spray()
        {
            if (inc < sumSpray * INC_ABILITY)
            {
                if (produced[PROLIFERATOR_MK3] > 0)
                {
                    int count = Math.Min(
                        (sumSpray * INC_ABILITY - inc - 1) / (INC_SPRAY_TIMES + EXTRA_INC_SPRAY_TIMES - 1) / INC_ABILITY + 1
                        , produced[PROLIFERATOR_MK3]);
                    inc += count * (INC_SPRAY_TIMES + EXTRA_INC_SPRAY_TIMES - 1) * INC_ABILITY;
                    produced[PROLIFERATOR_MK3] -= count;
                    served[PROLIFERATOR_MK3] += count;
                    consumedProliferator += count;
                }
            }
            if (inc < 1)
            {
                spray = false;
            }
        }
    }
}
