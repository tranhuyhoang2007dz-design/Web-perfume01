using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Web_perfume.Models;

namespace Web_perfume.Helpers
{
    public static class CartHelper
    {
        private const string CartSessionKey = "PERFUME_CART_SESSION";

        public static CartViewModel GetCart(HttpSessionStateBase session, PerfumeShopDBEntities db)
        {
            var user = AuthHelper.GetUser(session);
            CartViewModel cart = new CartViewModel();

            if (user != null)
            {
                // Retrieve user's cart from Database
                var dbCart = db.Carts.FirstOrDefault(c => c.UserID == user.UserID);
                if (dbCart != null)
                {
                    cart.Items = dbCart.CartItems.Select(ci => new CartItemViewModel
                    {
                        CartItemID = ci.CartItemID,
                        VariantID = ci.VariantID,
                        ProductID = ci.ProductVariant.ProductID,
                        ProductName = ci.ProductVariant.Product.ProductName,
                        Slug = ci.ProductVariant.Product.Slug,
                        BrandName = ci.ProductVariant.Product.Brand.BrandName,
                        ImageUrl = ci.ProductVariant.Product.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageURL 
                                   ?? ci.ProductVariant.Product.ProductImages.FirstOrDefault()?.ImageURL,
                        VolumeML = ci.ProductVariant.VolumeML,
                        SKU = ci.ProductVariant.SKU,
                        Price = ci.ProductVariant.Price,
                        SalePrice = ci.ProductVariant.SalePrice,
                        Quantity = ci.Quantity,
                        StockQuantity = ci.ProductVariant.StockQuantity
                    }).ToList();
                    return cart;
                }
            }

            // Fallback or guest user: Session-based cart
            var sessionCart = session[CartSessionKey] as List<CartItemViewModel>;
            if (sessionCart != null)
            {
                cart.Items = sessionCart;
            }
            return cart;
        }

        public static void SaveSessionCart(HttpSessionStateBase session, List<CartItemViewModel> items)
        {
            session[CartSessionKey] = items;
        }

        public static void AddItem(HttpSessionStateBase session, PerfumeShopDBEntities db, int variantId, int quantity)
        {
            if (quantity <= 0) quantity = 1;

            var variant = db.ProductVariants.Find(variantId);
            if (variant == null) return;

            var user = AuthHelper.GetUser(session);
            if (user != null)
            {
                var dbCart = db.Carts.FirstOrDefault(c => c.UserID == user.UserID);
                if (dbCart == null)
                {
                    dbCart = new Cart
                    {
                        UserID = user.UserID,
                        CreatedAt = DateTime.Now
                    };
                    db.Carts.Add(dbCart);
                    db.SaveChanges();
                }

                var existingItem = dbCart.CartItems.FirstOrDefault(ci => ci.VariantID == variantId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    dbCart.CartItems.Add(new CartItem
                    {
                        CartID = dbCart.CartID,
                        VariantID = variantId,
                        Quantity = quantity
                    });
                }
                db.SaveChanges();
            }
            else
            {
                var items = session[CartSessionKey] as List<CartItemViewModel> ?? new List<CartItemViewModel>();
                var existing = items.FirstOrDefault(i => i.VariantID == variantId);
                if (existing != null)
                {
                    existing.Quantity += quantity;
                }
                else
                {
                    items.Add(new CartItemViewModel
                    {
                        CartItemID = items.Count + 1,
                        VariantID = variant.VariantID,
                        ProductID = variant.ProductID,
                        ProductName = variant.Product.ProductName,
                        Slug = variant.Product.Slug,
                        BrandName = variant.Product.Brand.BrandName,
                        ImageUrl = variant.Product.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageURL 
                                   ?? variant.Product.ProductImages.FirstOrDefault()?.ImageURL,
                        VolumeML = variant.VolumeML,
                        SKU = variant.SKU,
                        Price = variant.Price,
                        SalePrice = variant.SalePrice,
                        Quantity = quantity,
                        StockQuantity = variant.StockQuantity
                    });
                }
                session[CartSessionKey] = items;
            }
        }

        public static void UpdateQuantity(HttpSessionStateBase session, PerfumeShopDBEntities db, int variantId, int quantity)
        {
            var user = AuthHelper.GetUser(session);
            if (user != null)
            {
                var dbCart = db.Carts.FirstOrDefault(c => c.UserID == user.UserID);
                if (dbCart != null)
                {
                    var item = dbCart.CartItems.FirstOrDefault(ci => ci.VariantID == variantId);
                    if (item != null)
                    {
                        if (quantity <= 0)
                            db.CartItems.Remove(item);
                        else
                            item.Quantity = quantity;
                        db.SaveChanges();
                    }
                }
            }
            else
            {
                var items = session[CartSessionKey] as List<CartItemViewModel>;
                if (items != null)
                {
                    var item = items.FirstOrDefault(i => i.VariantID == variantId);
                    if (item != null)
                    {
                        if (quantity <= 0)
                            items.Remove(item);
                        else
                            item.Quantity = quantity;
                        session[CartSessionKey] = items;
                    }
                }
            }
        }

        public static void RemoveItem(HttpSessionStateBase session, PerfumeShopDBEntities db, int variantId)
        {
            UpdateQuantity(session, db, variantId, 0);
        }

        public static void ClearCart(HttpSessionStateBase session, PerfumeShopDBEntities db)
        {
            var user = AuthHelper.GetUser(session);
            if (user != null)
            {
                var dbCart = db.Carts.FirstOrDefault(c => c.UserID == user.UserID);
                if (dbCart != null)
                {
                    db.CartItems.RemoveRange(dbCart.CartItems);
                    db.SaveChanges();
                }
            }
            session.Remove(CartSessionKey);
        }
    }
}

