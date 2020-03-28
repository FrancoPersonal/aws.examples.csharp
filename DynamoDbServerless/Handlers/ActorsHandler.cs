using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using DynamoDbServerless.Models;
using DynamoDbServerless.Services;
using Models;

namespace DynamoDbServerless.Handlers
{
    public class ActorsHandler
    {
        private readonly IDynamoDbReader _dynamoDbReader;
        private readonly IJsonConverter _jsonConverter;
        private readonly ILogger _logger;

        public ActorsHandler() : this(null, null, null)
        {
        }

        public ActorsHandler(IDynamoDbReader dynamoDbReader, IJsonConverter jsonConverter, ILogger logger)
        {
            _dynamoDbReader = dynamoDbReader ?? new DynamoDbReader();
            _jsonConverter = jsonConverter ?? new JsonConverter();
            _logger = logger ?? new Logger();
        }

        public async Task<APIGatewayProxyResponse> QueryActors(APIGatewayProxyRequest request, ILambdaContext context)
        {
            context.Logger.LogLine($"Query request: {_jsonConverter.SerializeObject(request)}");

            var requestBody = _jsonConverter.DeserializeObject<ActorsSearchRequest>(request.Body);
            if (string.IsNullOrEmpty(requestBody.FirstName))
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = "FirstName is mandatory"
                };
            }
            var queryRequest = BuildQueryRequest(requestBody.FirstName, requestBody.LastName);
            _logger.LogInformation("QueryActors invoked with {FirstName} and {LastName}, {@Content}",
                requestBody.FirstName, requestBody.LastName, requestBody);

            var response = await _dynamoDbReader.QueryAsync(queryRequest);
            context.Logger.LogLine($"Query result: {_jsonConverter.SerializeObject(response)}");

            var queryResults = BuildActorsResponse(response);
            _logger.LogInformation("QueryActors result for {FirstName} and {LastName} is {@Content}",
                requestBody.FirstName, requestBody.LastName, queryResults);

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = _jsonConverter.SerializeObject(queryResults)
            };
        }

        private static QueryRequest BuildQueryRequest(string firstName, string lastName)
        {
            var request = new QueryRequest("Actors")
            {
                KeyConditionExpression = "FirstName = :FirstName"
            };
            request.ExpressionAttributeValues.Add(":FirstName", new AttributeValue { S = firstName });

            if (!string.IsNullOrEmpty(lastName))
            {
                request.KeyConditionExpression += " AND #LastName = :LastName";
                request.ExpressionAttributeNames.Add("#LastName", "LastName");
                request.ExpressionAttributeValues.Add(":LastName", new AttributeValue { S = lastName });
            }

            return request;
        }

        private static ActorsSearchResponse BuildActorsResponse(QueryResponse response)
        {
            var actors = response.Items
                .Select(item => new Actor
                {
                    FirstName = item["FirstName"].S,
                    LastName = item["LastName"].S
                })
                .ToList();

            return new ActorsSearchResponse
            {
                Actors = actors,
                Count = actors.Count
            };
        }
    }
}