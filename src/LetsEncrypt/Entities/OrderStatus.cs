using System.ComponentModel.DataAnnotations;

namespace MaksIT.LetsEncrypt.Entities
{
    public enum OrderStatus
    {
        [Display(Name = "pending")]
        Pending,
        [Display(Name = "valid")]
        Valid,
        [Display(Name = "ready")]
        Ready,
        [Display(Name = "processing")]
        Processing
    }

    public static class OrderStatusExtensions
    {
        public static string GetDisplayName(this OrderStatus status)
        {
            var type = typeof(OrderStatus);
            var memInfo = type.GetMember(status.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(DisplayAttribute), false);
            return attributes.Length > 0 ? ((DisplayAttribute)attributes[0]).Name : status.ToString();
        }
    }
}
