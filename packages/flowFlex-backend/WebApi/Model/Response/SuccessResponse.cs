namespace FlowFlex.WebApi.Model.Response
{
    public class SuccessResponse
    {
        public int Code { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }

        public static SuccessResponse Create()
        {
            return new SuccessResponse
            {
                Code = 200,
                Status = "success",
                Message = "OK",
                Data = null
            };
        }

        public static SuccessResponse Create<T>(T data)
        {
            return new SuccessResponse
            {
                Code = 200,
                Status = "success",
                Message = "OK",
                Data = data
            };
        }
    }
}
