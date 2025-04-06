using AutoMapper;
using DDDProject.Application.Products.GetProductById; // For ProductResponse
using DDDProject.Domain.Entities;

namespace DDDProject.Application.Products.Mapping;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        // Map from Product entity to ProductResponse DTO
        CreateMap<Product, ProductResponse>()
            .ForMember(dest => dest.PriceAmount, opt => opt.MapFrom(src => src.Price.Amount))
            .ForMember(dest => dest.PriceCurrency, opt => opt.MapFrom(src => src.Price.Currency))
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.Sku.Value));

        // Add other mappings related to Products if needed
    }
} 