using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;

namespace Test
{
    public class TestMappingProfile : Profile
    {
        public TestMappingProfile()
		{
			AllowNullCollections = false;

			CreateMap<Class1, Class2>()
				.ForMember(dest => dest.Field1, opt => opt.MapFrom(src => src.Field1))
				.ForMember(dest => dest.Field2, opt => {
					opt.PreCondition(src => src.Field2 != null);
					opt.MapFrom(src => src.Field2);
				})
				.ForAllOtherMembers(opt => opt.Ignore());

			
		}
    }

	public class Class1
	{
		public int Field1 { get; set; } = 5;
		public string Field2 { get; set; } = "kek";
		public string Field3 { get; set; } = "test";
		public decimal Field4 { get; set; } = 3.3M;
	}

	public class Class2
	{
		public int Field1 { get; set; }
		public string Field2 { get; set; }
		public string Field3 { get; set; }
		public decimal Field4 { get; set; }
	}
}