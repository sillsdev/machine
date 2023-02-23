namespace SIL.Machine.WebApi.Controllers
{
    public class InternalServerErrorExceptionFilter : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            Console.WriteLine(context.Exception.ToString());
            base.OnException(context);
        }
    }
}
