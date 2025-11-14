using AutoMapper;
using Web_ban_do_thu_cong_my_nghe.Controllers;
using Web_ban_do_thu_cong_my_nghe.ViewModels;
using Web_ban_do_thu_cong_my_nghe.Data;

namespace Web_ban_do_thu_cong_my_nghe.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile() 
        {
            CreateMap<RegisterVM, User>()
    .ForMember(dest => dest.Fullname, opt => opt.MapFrom(src => src.HoTen))
    .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.MatKhau))
    .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.DiaChi))
    .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.DienThoai))
    .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.GioiTinh));

        }
    }
}
