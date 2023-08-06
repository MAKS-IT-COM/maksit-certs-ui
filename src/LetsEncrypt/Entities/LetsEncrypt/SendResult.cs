namespace MaksIT.LetsEncrypt.Entities {
  public class SendResult<TResult> {

    public TResult? Result { get; set; }

    public string? ResponseText { get; set; }


  }
}
