using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SimpleEcommerce.Data;
using SimpleEcommerce.Models;

namespace SimpleEcommerce.Services;

/// <summary>
/// Черновик сервиса рекомендаций товаров на основе избранного пользователя через OpenAI Embeddings.
/// Вся логика закомментирована — раскомментируйте и подключите при необходимости.
/// API-ключ брать из конфигурации: OpenAI:ApiKey (appsettings.json или переменная окружения).
/// </summary>
public class ProductRecommendationService
{
	// private readonly IConfiguration _config;
	// private readonly ApplicationDbContext _context;
	// private readonly IHttpClientFactory _httpClientFactory;
	// private const string EmbeddingsModel = "text-embedding-3-small";
	// private const int MaxRecommendations = 8;

	// public ProductRecommendationService(
	// 	IConfiguration config,
	// 	ApplicationDbContext context,
	// 	IHttpClientFactory httpClientFactory)
	// {
	// 	_config = config;
	// 	_context = context;
	// 	_httpClientFactory = httpClientFactory;
	// }

	/// <summary>
	/// Рекомендуемые товары по избранному пользователя (на основе эмбеддингов OpenAI).
	/// Возвращает ID товаров, которых нет в избранном, отсортированные по релевантности.
	/// </summary>
	// public async Task<List<int>> GetRecommendedProductIdsAsync(
	// 	int userId,
	// 	List<Product> allProducts,
	// 	List<int> favoriteProductIds,
	// 	CancellationToken ct = default)
	// {
	// 	if (favoriteProductIds.Count == 0)
	// 		return new List<int>();
	//
	// 	var apiKey = _config["OpenAI:ApiKey"];
	// 	if (string.IsNullOrWhiteSpace(apiKey))
	// 		return new List<int>();
	//
	// 	var favoriteProducts = allProducts.Where(p => favoriteProductIds.Contains(p.Id)).ToList();
	// 	var userProfileText = BuildUserProfileText(favoriteProducts);
	//
	// 	var client = _httpClientFactory.CreateClient();
	// 	client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
	//
	// 	// Эмбеддинг «профиля» пользователя (объединённые названия и описания избранных товаров)
	// 	var userEmbedding = await GetEmbeddingAsync(client, userProfileText, ct);
	// 	if (userEmbedding == null) return new List<int>();
	//
	// 	var favoriteSet = favoriteProductIds.ToHashSet();
	// 	var candidates = allProducts.Where(p => !favoriteSet.Contains(p.Id)).ToList();
	// 	if (candidates.Count == 0) return new List<int>();
	//
	// 	// Эмбеддинги кандидатов (можно кэшировать по ProductId в БД/Redis)
	// 	var scores = new List<(int ProductId, double Score)>();
	// 	foreach (var product in candidates)
	// 	{
	// 		var text = $"{product.Name}. {product.Description}";
	// 		var emb = await GetEmbeddingAsync(client, text, ct);
	// 		if (emb == null) continue;
	// 		var cos = CosineSimilarity(userEmbedding, emb);
	// 		scores.Add((product.Id, cos));
	// 	}
	//
	// 	return scores
	// 		.OrderByDescending(x => x.Score)
	// 		.Take(MaxRecommendations)
	// 		.Select(x => x.ProductId)
	// 		.ToList();
	// }

	// private static string BuildUserProfileText(List<Product> favoriteProducts)
	// {
	// 	var sb = new StringBuilder();
	// 	foreach (var p in favoriteProducts)
	// 		sb.AppendLine($"{p.Name}. {p.Description ?? ""}");
	// 	return sb.ToString().Trim();
	// }

	// private static async Task<float[]?> GetEmbeddingAsync(HttpClient client, string text, CancellationToken ct)
	// {
	// 	var body = new { input = text, model = EmbeddingsModel };
	// 	var response = await client.PostAsJsonAsync("https://api.openai.com/v1/embeddings", body, ct);
	// 	if (!response.IsSuccessStatusCode) return null;
	// 	var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
	// 	var data = json.GetProperty("data");
	// 	if (data.GetArrayLength() == 0) return null;
	// 	var embedding = data[0].GetProperty("embedding");
	// 	var list = new List<float>();
	// 	foreach (var e in embedding.EnumerateArray())
	// 		list.Add((float)e.GetDouble());
	// 	return list.ToArray();
	// }

	// private static double CosineSimilarity(float[] a, float[] b)
	// {
	// 	if (a.Length != b.Length || a.Length == 0) return 0;
	// 	double dot = 0, na = 0, nb = 0;
	// 	for (int i = 0; i < a.Length; i++)
	// 	{
	// 		dot += a[i] * b[i];
	// 		na += a[i] * a[i];
	// 		nb += b[i] * b[i];
	// 	}
	// 	var denom = Math.Sqrt(na) * Math.Sqrt(nb);
	// 	return denom > 0 ? dot / denom : 0;
	// }
}
