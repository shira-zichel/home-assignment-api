using AutoMapper;
using HomeAssignment.DTOs;
using HomeAssignment.Models;


namespace HomeAssignment.Profiles
{
    public class DataProfile : Profile
    {
        public DataProfile()
        {
            CreateMap<DataItem, DataItemDto>();
            CreateMap<CreateDataItemDto, DataItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
            CreateMap<UpdateDataItemDto, DataItem>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
        }
    }
}
