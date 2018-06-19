namespace UpgradedVehicles
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using UnityEngine;

    public class SeaMothStorageDeluxe : MonoBehaviour
    {

        public readonly SeamothStorageContainer[] Storages;

        public SeaMothStorageDeluxe()
        {
            Storages = new SeamothStorageContainer[4]
                        {
                            new SeamothStorageContainer(),
                            new SeamothStorageContainer(),
                            new SeamothStorageContainer(),
                            new SeamothStorageContainer()
                        };
        }

        public ItemsContainer GetStorageInSlot(int slotID)
        {
            return Storages[slotID].container;
        }
    }
}
