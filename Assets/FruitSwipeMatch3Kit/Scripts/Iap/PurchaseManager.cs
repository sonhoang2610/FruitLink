﻿// Copyright (C) 2019 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System;
using UnityEngine;
#if FRUIT_SWIPE_ENABLE_IAP
using UnityEngine.Purchasing;
#endif

namespace FruitSwipeMatch3Kit
{
    /// <summary>
    /// This class manages the in-app purchases of the game. It is based on the official Unity IAP
    /// documentation available here: https://docs.unity3d.com/Manual/UnityIAPInitialization.html
    /// </summary>
    #if FRUIT_SWIPE_ENABLE_IAP
    public class PurchaseManager : MonoBehaviour, IStoreListener
    {
    #else 
    public class PurchaseManager : MonoBehaviour
    {
    #endif
        public GameConfiguration GameConfig;
        public CoinsSystem CoinsSystem;
#if FRUIT_SWIPE_ENABLE_IAP    
        private static PurchaseManager _instance;
        public static PurchaseManager Instance => _instance;
        public IStoreController Controller { get; private set; }

        private void Awake()
        {
            if (_instance != null) Destroy(gameObject);
            else _instance = this;
        }

        private void Start()
        {
            DontDestroyOnLoad(gameObject);
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (var item in GameConfig.IapItems)
                builder.AddProduct(item.StoreId, ProductType.Consumable);
            UnityPurchasing.Initialize(this, builder);
        }

        /// <summary>
        /// Called when Unity IAP is ready to make purchases.
        /// </summary>
        /// <param name="storeController">The store controller.</param>
        /// <param name="extensionProvider">The extension provider.</param>
        public void OnInitialized(IStoreController storeController, IExtensionProvider extensionProvider)
        {
            Controller = storeController;
        }

        /// <summary>
        /// Called when Unity IAP encounters an unrecoverable initialization error.
        ///
        /// Note that this will not be called if Internet is unavailable; Unity IAP
        /// will attempt initialization until it becomes available.
        /// </summary>
        /// <param name="error">The error received.</param>
        public void OnInitializeFailed(InitializationFailureReason error)
        {
        }

        /// <summary>
        /// Called when a purchase completes.
        ///
        /// May be called at any time after OnInitialized().
        /// </summary>
        /// <param name="e">The purchase event arguments.</param>
        /// <returns>The processing result of the purchase.</returns>
        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs e)
        {
            var purchasedProductId = e.purchasedProduct.definition.id;
            var catalogProduct =
                GameConfig.IapItems.Find(x => x.StoreId == purchasedProductId);
            if (catalogProduct != null)
            {
                PlayerPrefs.SetInt(GameplayConstants.NoAdsPrefKey, 1);
                CoinsSystem.BuyCoins(catalogProduct.NumCoins);
			    var shopPopup = FindObjectOfType<BuyCoinsPopup>();
                if (shopPopup != null)
                {
                    shopPopup.GetComponent<BuyCoinsPopup>().CloseLoadingPopup();
                    shopPopup.GetComponent<BuyCoinsPopup>().ParentScreen.OpenPopup<AlertPopup>("Popups/AlertPopup",
                        popup =>
                        {
                            popup.SetText($"You purchased {catalogProduct.NumCoins} coins!");
                        });
                }
            }
            return PurchaseProcessingResult.Complete;
        }

        /// <summary>
        /// Called when a purchase fails.
        /// </summary>
        /// <param name="product">The product.</param>
        /// <param name="reason">The failure reason.</param>
        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
        {
            var shopPopup = FindObjectOfType<BuyCoinsPopup>();
            if (shopPopup != null)
            {
                shopPopup.GetComponent<BuyCoinsPopup>().CloseLoadingPopup();
                shopPopup.GetComponent<BuyCoinsPopup>().ParentScreen.OpenPopup<AlertPopup>("Popups/AlertPopup",
                    popup =>
                    {
                        popup.SetText(
                            $"There was an error when purchasing:\n{GetPurchaseFailureReasonString(reason)}");
                    });
            }
        }

        /// <summary>
        /// Returns a readable string of the specified purchase failure reason.
        /// </summary>
        /// <param name="reason">The purchase failure reason.</param>
        /// <returns>A readable string of the specified purchase failure reason.</returns>
        private string GetPurchaseFailureReasonString(PurchaseFailureReason reason)
        {
            switch (reason)
            {
                case PurchaseFailureReason.DuplicateTransaction:
                    return "Duplicate transaction.";

                case PurchaseFailureReason.ExistingPurchasePending:
                    return "Existing purchase pending.";

                case PurchaseFailureReason.PaymentDeclined:
                    return "Payment was declined.";

                case PurchaseFailureReason.ProductUnavailable:
                    return "Product is not available.";

                case PurchaseFailureReason.PurchasingUnavailable:
                    return "Purchasing is not available.";

                case PurchaseFailureReason.SignatureInvalid:
                    return "Invalid signature.";

                case PurchaseFailureReason.Unknown:
                    return "Unknown error.";

                case PurchaseFailureReason.UserCancelled:
                    return "User cancelled.";

            }

            return "Unknown error.";
        }
        #endif    
    }
}
