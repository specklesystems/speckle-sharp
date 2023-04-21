using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Abstractions.Websocket;
using Speckle.Newtonsoft.Json;
using Speckle.Newtonsoft.Json.Serialization;

namespace Speckle.Core.Api.GraphQL.Serializer;

public class NewtonsoftJsonSerializer : IGraphQLWebsocketJsonSerializer
{
  public NewtonsoftJsonSerializer()
    : this(DefaultJsonSerializerSettings) { }

  public NewtonsoftJsonSerializer(Action<JsonSerializerSettings> configure)
    : this(configure.AndReturn(DefaultJsonSerializerSettings)) { }

  public NewtonsoftJsonSerializer(JsonSerializerSettings jsonSerializerSettings)
  {
    JsonSerializerSettings = jsonSerializerSettings;
    ConfigureMandatorySerializerOptions();
  }

  public static JsonSerializerSettings DefaultJsonSerializerSettings =>
    new()
    {
      ContractResolver = new CamelCasePropertyNamesContractResolver { IgnoreIsSpecifiedMembers = true },
      MissingMemberHandling = MissingMemberHandling.Ignore,
      Converters = { new ConstantCaseEnumConverter() }
    };

  public JsonSerializerSettings JsonSerializerSettings { get; }

  public string SerializeToString(GraphQLRequest request)
  {
    return JsonConvert.SerializeObject(request, JsonSerializerSettings);
  }

  public byte[] SerializeToBytes(GraphQLWebSocketRequest request)
  {
    var json = JsonConvert.SerializeObject(request, JsonSerializerSettings);
    return Encoding.UTF8.GetBytes(json);
  }

  public Task<WebsocketMessageWrapper> DeserializeToWebsocketResponseWrapperAsync(System.IO.Stream stream)
  {
    return DeserializeFromUtf8Stream<WebsocketMessageWrapper>(stream);
  }

  public GraphQLWebSocketResponse<GraphQLResponse<TResponse>> DeserializeToWebsocketResponse<TResponse>(byte[] bytes)
  {
    return JsonConvert.DeserializeObject<GraphQLWebSocketResponse<GraphQLResponse<TResponse>>>(
      Encoding.UTF8.GetString(bytes),
      JsonSerializerSettings
    );
  }

  public Task<GraphQLResponse<TResponse>> DeserializeFromUtf8StreamAsync<TResponse>(
    System.IO.Stream stream,
    CancellationToken cancellationToken
  )
  {
    return DeserializeFromUtf8Stream<GraphQLResponse<TResponse>>(stream);
  }

  // deserialize extensions to Dictionary<string, object>
  private void ConfigureMandatorySerializerOptions()
  {
    JsonSerializerSettings.Converters.Insert(0, new MapConverter());
  }

  private Task<T> DeserializeFromUtf8Stream<T>(System.IO.Stream stream)
  {
    using var sr = new StreamReader(stream);
    using JsonReader reader = new JsonTextReader(sr);
    var serializer = JsonSerializer.Create(JsonSerializerSettings);
    return Task.FromResult(serializer.Deserialize<T>(reader));
  }
}
