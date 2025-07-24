namespace MusicBeePlugin
{
    public class Response
    {
        public Response(bool success, string message) 
        {
            this.Success = success; 
            this.Message = message;
        }

        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
