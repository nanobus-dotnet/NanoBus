# NanoBus
NanoBus is a lightweight, AOT-first service bus intended to be lightweight, fast startup, use low memory footprint, and naturally use no reflection. 

This project was started due to there currently being no other service bus frameworks that are AOT-first. In other words, they still depend on reflection. This causes the performance of these frameworks to not be ideal for applications that demand the best performance possible. This also causes startup of applications using these frameworks to be slow which isn't ideal for containerization and cloud platforms where applications are being constantly spun up and down. 

Also, reflection code often produces runtime bugs and issues that are hard to debug and reproduce. This can be a nightmare for production. In NanoBus, there will be less surprises because if any reflection code is introduced, it will not compile. If reflection is required in your project, you may still reference the NanoBus AOT libraries.

Since reflection can't be supported in AOT code, message handlers cannot be discovered dynamically at runtime. **-HOWEVER-** this is where NanoBus code generation steps in and automatically wires up message handlers.

---
## NanoBus Code Structure
The rough structure of NanoBus code is the following:
- `NanoBus.Abstractions` - Core abstractions necessary for all NanoBus implementations.
- `NanoBus.Core` - Core implementations.
- `NanoBus.Core.DependencyInjection` - Extensions for core dependency injection.
- `NanoBus.Transports.Demo` - Simple implementation for demoing/learning NanoBus.
- `NanoBus.Transports.Demo.DependencyInjection` - Extensions for the demo transport dependency injection.
- `NanoBus.Transports.Demo.SourceGeneration` - Source generation for the demo transport implementation.

Other NanoBus enhancements and transport implementations will follow. Outbox and saga support may be implemented depending on demand or time available.

*For details on contributing, see [CONTRIBUTING.md](./CONTRIBUTING.md)*

*For the code of conduct, see [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md)*

*For the list of contributors, see [CONTRIBUTORS.md](./CONTRIBUTORS.md)*
