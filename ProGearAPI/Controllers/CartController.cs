﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ProGearAPI.Models.EF;

namespace ProGearAPI.Controllers
{
    //Selects userId using the userEmail 
    [ApiController]
    [Route("Cart")]
    public class CartController : ControllerBase
    {
        ProGearContext dbContext = new ProGearContext();

        [HttpGet]
        [Route("get-user-ID/{userEmail}")]
        public IActionResult getUserIdUsingEmail(string userEmail)
        {
            try
            {
                var i = (from x in dbContext.Users
                            where x.Email == userEmail
                            select x.UserId).SingleOrDefault();

                if (i > 0)
                {
                    return Ok(i); 
                }
                else{
                    return BadRequest("Invalid email.");
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        //Allows quantity of an order to be changed
        [HttpPut]
        [Route("set-order-qty/{orderID}/{newQty}")]
        public IActionResult modifyOrderQuantity(int orderID, int newQty)
        {
            // TODO: if newQty < 1, delete order
            try
            {
                var order = (from x in dbContext.Orders
                            where x.OrderId == orderID
                            select x).SingleOrDefault();

                if (order != null)
                {
                    order.Qty = newQty;

                    dbContext.Update(order);
                    dbContext.SaveChanges();
                    return Ok("Order quantity updated."); 
                }
                else{
                    return BadRequest("Invalid Order ID.");
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        //Removes single order from ordertable by using its id
        [HttpDelete]
        [Route("remove-order/{orderID}")]
        public IActionResult removeOrder(int orderID)
        {
            var order = (from x in dbContext.Orders
                       where x.OrderId == orderID
                       select x).SingleOrDefault();

            if (order != null)
            {
                dbContext.Orders.Remove(order);
                dbContext.SaveChanges();
                return Ok("Order deleted.");
            }
            else
            {
                return BadRequest("Invalid Order ID.");
            }
        }


        double v;
        double V;

        //Gets all wanted information from a Cart using a specific Cart id
        #region Get Cart By Id
        [HttpGet]
        [Route("Cart/{cartId}")]
        public IActionResult GetCartById(int cartId)
        {
            try
            {


                var cart = (from i in dbContext.Carts
                            join x in dbContext.Users on i.UserId equals x.UserId
                            join z in dbContext.Orders on i.CartId equals z.CartId
                            join w in dbContext.Products on z.ProductId equals w.ProductId
                            //orderby i.CartId ascending
                            where i.CartId == cartId
                            select new
                            {
                                i.CartId,
                                i.UserId,
                                i.PaidFor,
                                i.PaidOn,
                                //i.User,
                                //i.Orders,
                                z.OrderId,
                                w.ProductId,
                                w.ProductName,
                                w.ProductDetails,
                                w.ProductPrice,
                                z.Qty,
                                SubTotal = z.Qty * w.ProductPrice
                            }
                             ).DefaultIfEmpty();

                var total = cart.Sum(V => V.SubTotal);
               //Console.WriteLine("Total" , total);

                var final = (from i in dbContext.Carts
                            join x in dbContext.Users on i.UserId equals x.UserId
                            join z in dbContext.Orders on i.CartId equals z.CartId
                            join w in dbContext.Products on z.ProductId equals w.ProductId
                            
                            where i.CartId == cartId
                            select new
                            {
                                i.CartId,
                                total,
                                i.UserId,
                                i.PaidFor,
                                i.PaidOn,
                                //i.User,
                                //i.Orders,
                                z.OrderId,
                                w.ProductId,
                                w.ProductName,
                                w.ProductDetails,
                                w.ProductPrice,
                                z.Qty,
                                SubTotal = z.Qty * w.ProductPrice

                            }
                            ).DefaultIfEmpty();


                if (cart != null)
                {
                    UpdateCart(total, cartId); 
                    return Ok(final);
                }
                else
                {
                    return NotFound("No Cart");
                }
            }
            catch (Exception es)
            {
                throw new Exception(es.Message);

            }
        }
        #endregion
        
        //Used to update the total of the cart by adding the subtotal of each order
        #region Update Cart
        [HttpPut]
        [Route("UpdateCart")]
        public IActionResult UpdateCart(double? total, int cartId)
        {
            var update = (from i in dbContext.Carts
                          where i.CartId == cartId
                          select i).SingleOrDefault();

            if (update != null)
            {
                update.Total = total;
                dbContext.SaveChanges();
                return Ok("Updated Total");
            }
            else
            {
                return Ok("Update Failed");
            }
        }
        #endregion

        //Create a New Cart for a user typically done on account creation and after a cart is considered checked out
        #region Create New Cart
        [HttpPost]
        [Route("NewCart")]
        public IActionResult CreateCart(int userId)
        {
            var newCart = new Cart();

            newCart.UserId = userId;
            newCart.Total = 0;
            newCart.PaidFor = false;
            newCart.PaidOn = null;



            if (newCart != null)
            {
                dbContext.Carts.Add(newCart);
                dbContext.SaveChanges();

                return Created("", newCart);
            }
            else
            {
                return Ok("Error");
            }
        }
        #endregion

        

        //Adds an order from the cart using the cartId, productId, and qty
        [HttpPost]
        [Route("AddAnOrder")]
        public IActionResult AddOrder(int productId, int cartId, int qty)
        {

            Order newOrder = new Order();

            var check = (from i in dbContext.Orders
                         where i.ProductId == productId && i.CartId == cartId
                         select new
                         {
                             i.Qty
                         }).FirstOrDefault();

            if (check != null)
            {
                int? oldQty = check.Qty;

                var remove = (from i in dbContext.Orders
                              where i.ProductId == productId && i.CartId == cartId
                              select i).FirstOrDefault();

                if (remove != null)
                {
                    dbContext.Orders.Remove(remove);
                    dbContext.SaveChanges();

                    newOrder.ProductId = productId;
                    newOrder.CartId = cartId;
                    newOrder.Qty = qty + oldQty;

                    if (newOrder != null)
                    {
                        dbContext.Orders.Add(newOrder);
                        dbContext.SaveChanges();


                        return Created("", newOrder);
                    }
                    else
                    {
                        return Ok("Error");
                    }
                }
                else
                {
                    return BadRequest("Something Went Wrong");
                }

                
            }
            else
            {
                newOrder.ProductId = productId;
                newOrder.CartId = cartId;
                newOrder.Qty = qty;
            }

            if (newOrder != null)
            {
                dbContext.Orders.Add(newOrder);
                dbContext.SaveChanges();


                return Created("", newOrder);
            }
            else
            {
                return Ok("Error");
            }

        }

        [HttpGet]
        [Route("CheckOrder")]
        public IActionResult CheckOrder(int productId)
        {
            var check = (from i in dbContext.Orders
                         where i.ProductId == productId
                         select new
                         {
                                i.Qty
                         }).DefaultIfEmpty();


            if (check != null)
            {
                return Ok(check);
            }
            else
            {
                return Ok(false);
            }
        }

        [HttpGet]
        [Route("Orders/{orderId}")]
        public IActionResult ViewOrders(int orderId)
        {
            var cart = (from i in dbContext.Orders
                        join x in dbContext.Products on i.ProductId equals x.ProductId
                        where i.OrderId == orderId
                        select new
                        {
                         i.OrderId,
                         i.ProductId,
                         i.CartId,
                         i.Qty,
                         x.ProductName,
                         x.ProductPrice,
                         x.ProductDetails 

                        }
                          ).DefaultIfEmpty();

            if (cart != null)
            {
                return Ok(cart);
            }
            else
            {
                return NotFound("No Cart");
            }
        }

        [HttpGet]
        [Route("OrdersBy/{cartId}")]
        public IActionResult ViewOrdersByCartId(int cartId)
        {
            var cart = (from i in dbContext.Orders
                        join x in dbContext.Products on i.ProductId equals x.ProductId
                        where i.CartId == cartId
                        select new
                        {
                            i.OrderId,
                            i.ProductId,
                            i.CartId,
                            i.Qty,
                            x.ProductName,
                            x.ProductPrice,
                            x.ProductDetails

                        }
                          ).DefaultIfEmpty();

            if (cart != null)
            {
                return Ok(cart);
            }
            else
            {
                return NotFound("No Cart");
            }
        }


    [HttpDelete]
    [Route("emptycart")]
        public IActionResult deleteCart(int cartId)
        {
            var cart = (from orders in dbContext.Carts
                        where orders.CartId == cartId
                        select orders).SingleOrDefault();
            if (cart != null)
            {
                dbContext.Carts.Remove(cart);
                dbContext.SaveChanges();
                return Ok("Your Cart Is Now Empty");

            }
            else
            {
                return BadRequest("Your Cart Is Empty");
            }
        }



        [HttpGet]
        [Route("Users")]
        public IActionResult GetUser()
        {
            var cart = from i in dbContext.Users
                       select i;

            return Ok(cart);
        }
        








    }
}
