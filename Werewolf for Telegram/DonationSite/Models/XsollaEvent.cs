using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DonationSite.Models.Xsolla
{
    public class XsollaEvent
    {
        public string notification_type { get; set; }
        public User user { get; set; }
        public Purchase purchase { get; set; }
        public Transaction transaction { get; set; }
        public PaymentDetails payment_details { get; set; }
        public RefundDetails refund_details { get; set; }
        public Subscription subscription { get; set; }
        public Coupon coupon { get; set; }
    }

    public class User
    {
        public string ip { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string country { get; set; }
        public string public_id { get; set; }
    }

    public class Purchase
    {
        public VirtualCurrency virtual_currency { get; set; }
        public Checkout checkout { get; set; }
        public Subscription subscription { get; set; }
        public VirtualItems virtual_items { get; set; }
        public PinCodes pin_codes { get; set; }
        public Gift gift { get; set; }
        public Total total { get; set; }
        public List<Promotions> promotions { get; set; }
        public Coupon coupon { get; set; }
    }

    public class VirtualCurrency
    {
        public string name { get; set; }
        public string sku { get; set; }
        public string quantuty { get; set; }
        public string currency { get; set; }
        public float amount { get; set; }
    }

    public class Checkout
    {
        public string currency { get; set; }
        public float amount { get; set; }
    }

    public class Subscription
    {
        public string plan_id { get; set; }
        public int subscription_id { get; set; }
        public string product_id { get; set; }
        public string[] tags { get; set; }
        public string date_create { get; set; }
        public string date_next_charge { get; set; }
        public string currency { get; set; }
        public float amount { get; set; }
        public Trial trial { get; set; }
    }

    public class Trial
    {
        public int value { get; set; }
        public string type { get; set; }
    }

    public class VirtualItems
    {
        public Item[] items { get; set; }
        public string currency { get; set; }
        public int amount { get; set; }
    }

    public class Item
    {
        public string sku { get; set; }
        public int amount { get; set; }
    }

    public class PinCodes
    {
        public string digital_content { get; set; }
        public string drm { get; set; }
        public string currency { get; set; }
        public float amount { get; set; }
    }
    public class Gift
    {
        public string giver_id { get; set; }
        public string receiver_id { get; set; }
        public string receiver_email { get; set; }
        public string message { get; set; }
        public string hide_giver_from_receiver { get; set; }
    }

    public class Total
    {
        public string currency { get; set; }
        public float amount { get; set; }
    }

    public class Promotions
    {
        public string technical_name { get; set; }
        public int id { get; set; }
    }

    public class Coupon
    {
        public string coupon_code { get; set; }
        public string campaign_code { get; set; }
    }

    public class Transaction
    {
        public int id { get; set; }
        public string external_id { get; set; }
        public string payment_date { get; set; }
        public int payment_method { get; set; }
        public int dry_run { get; set; }
        public int agreement { get; set; }
    }

    public class PaymentDetails
    {
        public Payment payment { get; set; }
        public PaymentMethodSum payment_method_sum { get; set; }
        public XsollaBalanceSum xsolla_balance_sum { get; set; }
        public Payout payout { get; set; }
        public Vat vat { get; set; }
        public float payout_currency_rate { get; set; }
        public XsollaFee xsolla_fee { get; set; }
        public PaymentMethodFee payment_method_fee { get; set; }
        public SalesTax sales_tax { get; set; }

    }

    public class Payment
    {
        public string currency { get; set; }
        public string amount { get; set; }
    }

    public class PaymentMethodSum
    {
        public string currency { get; set; }
        public string amount { get; set; }
    }

    public class XsollaBalanceSum
    {
        public string currency { get; set; }
        public string amount { get; set; }
    }

    public class Payout
    {
        public string currency { get; set; }
        public float amount { get; set; }
    }

    public class Vat
    {
        public string currency { get; set; }
        public float amount { get; set; }
    }

    public class XsollaFee
    {
        public string currency { get; set; }
        public float amount { get; set; }
    }

    public class PaymentMethodFee
    {
        public string currency { get; set; }
        public float amount { get; set; }
    }

    public class SalesTax
    {
        public string currency { get; set; }
        public float amount { get; set; }
    }

    public class RefundDetails
    {
        public int code { get; set; }
        public string reason { get; set; }
        public string author { get; set; }
    }
}
