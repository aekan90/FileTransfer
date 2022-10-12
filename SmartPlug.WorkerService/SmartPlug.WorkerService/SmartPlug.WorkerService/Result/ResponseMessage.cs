//using Newtonsoft.Json;
//using Serilog;
using System;

namespace SmartPlug.WorkerService
{
    [Serializable]
    public class ResponseMessage<T>
    {
        public ResponseMessage()
        {
            IsSuccess = false;
        }

        public T Data { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string DeveloperMessage { get; set; }
        public int StatusCode { get; set; }
        public Exception Exception { get; set; }
        //Static Factory Method
        public static ResponseMessage<T> Success(T data)
        {
            return new ResponseMessage<T> { Data = data, StatusCode = 200, IsSuccess = true };
        }

        //Static Factory Method
        public static ResponseMessage<T> Success(string message)
        {
            return new ResponseMessage<T> { Message = message, StatusCode = 200, IsSuccess = true };
        }

        public static ResponseMessage<T> Success(T data, string message)
        {
            return new ResponseMessage<T> { Data = data, Message = message, StatusCode = 200, IsSuccess = true };
        }

        public static ResponseMessage<T> Fail(string error)
        {
            //Log.Error("İşlem Hatası {@RequestData}", new
            //{
            //    RequestData = error
            //});

            return new ResponseMessage<T>
            {
                StatusCode = 200,
                IsSuccess = false,
                Message = error
            };
        }

        public static ResponseMessage<T> Fail<TData>(string error, TData model)
        {
            //string request = "null";
            //if (model != null)
            //{
            //    request = JsonConvert.SerializeObject(model);
            //}

            //Log.Error("İşlem Hatası {@RequestData} {@ServisHata}", new 
            //{ 
            //    RequestData = request,
            //    ServisHata = error
            //});

            return new ResponseMessage<T>
            {
                StatusCode = 200,
                IsSuccess = false,
                Message = error
            };
        }

        public static ResponseMessage<T> NoDataFound(string error)
        {
            return new ResponseMessage<T>
            {
                StatusCode = 200,
                IsSuccess = false,
                Message = error
            };
        }
    }
}
