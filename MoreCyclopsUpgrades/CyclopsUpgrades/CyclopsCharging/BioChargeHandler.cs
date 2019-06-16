﻿namespace MoreCyclopsUpgrades.CyclopsUpgrades.CyclopsCharging
{
    using MoreCyclopsUpgrades.API;
    using MoreCyclopsUpgrades.Caching;
    using MoreCyclopsUpgrades.Managers;
    using MoreCyclopsUpgrades.Modules;
    using MoreCyclopsUpgrades.Monobehaviors;
    using MoreCyclopsUpgrades.SaveData;
    using System.Collections.Generic;
    using UnityEngine;

    internal class BioChargeHandler : ICyclopsCharger
    {
        internal const float BatteryDrainRate = ChargeManager.BatteryDrainRate;
        private const float BioReactorRateLimiter = 0.90f;

        private readonly ChargeManager ChargeManager;
        private BioBoosterUpgradeHandler BioBoosters => ChargeManager.BioBoosters;

        private List<CyBioReactorMono> BioReactors => ChargeManager.CyBioReactors;

        public bool IsRenewable { get; } = false;

        internal readonly int MaxBioReactors = ModConfig.Settings.MaxBioReactors();
        internal bool ProducingPower = false;

        private float totalBioCharge = 0f;
        private float totalBioCapacity = 0f;

        private readonly Atlas.Sprite sprite = SpriteManager.Get(CyclopsModule.BioReactorBoosterID);

        public readonly SubRoot Cyclops;

        public BioChargeHandler(ChargeManager chargeManager)
        {
            ChargeManager = chargeManager;
            Cyclops = chargeManager.Cyclops;
        }

        public Atlas.Sprite GetIndicatorSprite()
        {
            return sprite;
        }

        public string GetIndicatorText()
        {
            return NumberFormatter.FormatNumber(Mathf.RoundToInt(totalBioCharge), NumberFormat.Amount);
        }

        public Color GetIndicatorTextColor()
        {
            return NumberFormatter.GetNumberColor(totalBioCharge, totalBioCapacity, 0f);
        }

        public bool HasPowerIndicatorInfo()
        {
            return ProducingPower;
        }

        public float ProducePower(float requestedPower)
        {
            if (this.BioReactors.Count == 0)
            {
                ProducingPower = false;
                return 0f;
            }

            float tempBioCharge = 0f;
            float tempBioCapacity = 0f;
            float charge = 0f;

            int poweredReactors = 0;
            foreach (CyBioReactorMono reactor in this.BioReactors)
            {
                if (!reactor.HasPower)
                    continue;

                if (poweredReactors < MaxBioReactors)
                {
                    poweredReactors++;

                    charge += reactor.GetBatteryPower(BatteryDrainRate * BioReactorRateLimiter, requestedPower);

                    tempBioCharge += reactor.Battery._charge;
                    tempBioCapacity = reactor.Battery._capacity;
                }
            }

            ProducingPower = poweredReactors > 0;

            totalBioCharge = tempBioCharge;
            totalBioCapacity = tempBioCapacity;

            return charge;
        }

        public float TotalReservePower()
        {
            throw new System.NotImplementedException();
        }
    }


}
