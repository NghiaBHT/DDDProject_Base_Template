using AutoMapper;
using DDDProject.Domain.Entities;
using DDDProject.Domain.Repositories;
// using DDDProject.Domain.Exceptions; // Could use ProductNotFoundException, but returning null might be preferred for queries
using MediatR;

namespace DDDProject.Application.Products.GetProductById;

internal sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductResponse?>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductByIdQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<ProductResponse?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            // Option 1: Return null (Controller can return NotFound)
            return null;

            // Option 2: Throw specific exception (Middleware handles NotFound response)
            // throw new ProductNotFoundException(request.ProductId);
        }

        // Map Entity to DTO
        return _mapper.Map<ProductResponse>(product);

        // Manual Mapping Example (if not using AutoMapper):
        // return new ProductResponse(
        //     product.Id,
        //     product.Name,
        //     product.Description,
        //     product.Price.Amount,
        //     product.Price.Currency,
        //     product.Sku.Value
        // );
    }
} 