using System.Collections.Generic;
using CampLantern.Estate;
using UnityEngine;

namespace CampLantern.Core.Persistence
{
    /// <summary>
    /// Wallet/Inventory/EstateShop을 소유하고 로컬 JSON과 동기화한다.
    /// 씬(공간)마다 새로 만들어지지만 Load()가 디스크의 마지막 저장 상태를 복원하므로
    /// 로비→낚시터→영지 이동에도 코인·아이템이 이어진다.
    /// 오프라인 영지 방문(다른 유저가 내 영지를 읽는 것)은 이걸로 해결되지 않는다 — 그건 서버가 필요
    /// (room-architecture.md, tech-stack-decisions.md "영구 저장 백엔드" 항목 참조).
    /// </summary>
    public class PlayerState
    {
        public Wallet Wallet { get; }
        public Inventory Inventory { get; }
        public EstateShop Shop { get; }

        /// <summary>Load() 직후 채워지는 배치 복원 목록 — 영지 씬이 EstateManager.Place로 적용한다.</summary>
        public IReadOnlyList<PlacedObjectSave> PendingPlacements { get; private set; } = new List<PlacedObjectSave>();

        public PlayerState()
        {
            Wallet    = new Wallet();
            Inventory = new Inventory();
            Shop      = new EstateShop(Wallet, Inventory);
        }

        /// <summary>디스크에서 로드해 Wallet/Inventory/Shop에 반영한다. 씬 시작 시 1회 호출.</summary>
        public void Load(ContentRegistry registry)
        {
            PlayerSaveData data = SaveService.Load();

            if (data.Coins > 0) Wallet.Add(data.Coins);

            foreach (ItemStackSave stack in data.Inventory)
            {
                if (registry.TryGetItem(stack.Id, out ItemDef def))
                    Inventory.Add(def, stack.Count);
                else
                    Debug.LogWarning($"[PlayerState] 저장된 아이템 Id를 찾을 수 없음(ContentRegistry 갱신 필요): {stack.Id}");
            }

            foreach (ItemStackSave stack in data.OwnedEstateDefs)
            {
                if (registry.TryGetEstateObject(stack.Id, out EstateObjectDef def))
                    for (int i = 0; i < stack.Count; i++) Shop.ReturnOwned(def); // 기존 공개 API로 보유 목록 복원
                else
                    Debug.LogWarning($"[PlayerState] 저장된 영지 오브젝트 Id를 찾을 수 없음(ContentRegistry 갱신 필요): {stack.Id}");
            }

            PendingPlacements = data.PlacedObjects;
        }

        /// <summary>
        /// 현재 상태를 디스크에 저장한다. estateManager가 있으면(영지 씬) 배치 목록도 함께 갱신하고,
        /// 없으면(낚시터/사냥터 씬) 디스크에 있던 배치 목록을 그대로 보존한다 — 이 씬은 배치를 모르므로
        /// 건드리면 안 됨.
        /// </summary>
        public void Save(EstateManager estateManager = null)
        {
            PlayerSaveData data = SaveService.Load(); // baseline — 이 씬이 모르는 필드(예: 배치 목록) 보존

            data.Coins = Wallet.Coins;

            data.Inventory.Clear();
            foreach (KeyValuePair<ItemDef, int> entry in Inventory.Items)
                data.Inventory.Add(new ItemStackSave { Id = entry.Key.Id, Count = entry.Value });

            data.OwnedEstateDefs.Clear();
            foreach (KeyValuePair<EstateObjectDef, int> entry in Shop.OwnedDefs)
                data.OwnedEstateDefs.Add(new ItemStackSave { Id = entry.Key.Id, Count = entry.Value });

            if (estateManager != null)
            {
                data.PlacedObjects.Clear();
                foreach (PlacedObject placed in estateManager.PlacedObjects)
                {
                    data.PlacedObjects.Add(new PlacedObjectSave
                    {
                        DefId    = placed.Def.Id,
                        Position = placed.transform.position,
                        Rotation = placed.transform.rotation,
                    });
                }
            }

            SaveService.Save(data);
        }
    }
}
