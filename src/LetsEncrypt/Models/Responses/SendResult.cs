namespace MaksIT.LetsEncrypt.Models.Responses
{
    public class SendResult<TResult>
    {

        public TResult? Result { get; set; }

        public string? ResponseText { get; set; }

  }
}
