using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WeddingPlanner.Models;

namespace WeddingPlanner {
    public class Startup {
        public IConfiguration Configuration { get; }
        public Startup (IConfiguration configuration) {
            Configuration = configuration;
        }

        public void ConfigureServices (IServiceCollection services) {
            services.AddDbContext<WeddingPlannerContext>(options => options.UseMySql(Configuration["DBInfo:ConnectionString"]));
            services.AddMvc ();
            services.AddSession ();
        }

        public void Configure (IApplicationBuilder app, IHostingEnvironment env) {
            if (env.IsDevelopment ()) {
                app.UseDeveloperExceptionPage ();
            }
            app.UseStaticFiles ();
            app.UseSession ();
            app.UseMvc ();
        }
    }
}