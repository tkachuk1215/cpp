namespace Core.Reports;

// Вхідна колекція переміщень (рух товару за id)
public sealed record StockMovement(string ProductId, int Delta);

// Join: одна пара «товар × переміщення»
public sealed record MovementLine(string Sku, int Delta);

// GroupJoin: зведення по товару (товари без переміщень теж є, з нулями)
public sealed record MovementSummary(string Sku, string Name, int Movements, int NetDelta);
