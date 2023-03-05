using AutoMapper;

namespace Test
{
	public static class TestMapper
	{
		private static Mapper _mapper;
		public static Mapper Mapper
		{
			get
			{
				if (_mapper == null)
				{
					var config =  new MapperConfiguration(cfg => cfg.AddProfile<TestMappingProfile>());
					_mapper = new Mapper(config);
				}

				return _mapper;
			}
		}
	}

    public class TestMappingProfile : Profile
    {
        public TestMappingProfile()
		{
			// CreateMap<Class1, Class2>()
			// 	.ForMember(dest => dest.Field1, opt => opt.MapFrom(src => src.Field1))
			// 	.ForMember(dest => dest.Field2, opt => {
			// 		opt.PreCondition(src => src.Field2 != null);
			// 		opt.MapFrom(src => src.Field2);
			// 	})
			// 	.IgnoreOther()
			// 	.ReverseMap()
			// 	.IgnoreOther();

			CreateMap<Class1, Class2>()
				.ForMember(dest => dest.Field1, opt => opt.MapFrom(src => src.Field2))
				.IgnoreOther();
		}
    }

	public static class MapperExtension
	{
		public static IMappingExpression<TSource, TDestination> IgnoreOther<TSource, TDestination>(this IMappingExpression<TSource, TDestination> map)
		{
			map.ForAllOtherMembers(opt => opt.Ignore());
			return map;
		}
	}

	public class Class1
	{
		public int Field1 { get; set; }
		public string Field2 { get; set; }
		public string Field3 { get; set; }
		public decimal Field4 { get; set; }
	}

	public class Class2
	{
		public int Field1 { get; set; }
		public string Field2 { get; set; }
		public string Field3 { get; set; }
		public decimal Field4 { get; set; }
	}
}