# BehaviorDesigner
这是1.5.5的反编译版本。
维护的目的是为了了解行为树，然后想做一个技能树。
其实感觉行为树更好一些，但是一直认为，规矩应该加的有个度。
行为树对于项目来说，太大，里面必然有一些东西是项目用不到的，而且为了实现这些功能，必然要做一些牺牲。
然后准备一个基于事件的框架，有点SkyNet的思想，但是底层打算用ECS的思想去实现。
其实一直也不喜欢框架这个概念。
不同的开发模型适合不同的开发模式，但是如果强加框架的概念的话，一来项目看起来特别臃肿，重构起来简直是灾难，二来对于开发人员熟悉起来成本也比较高。
而且最重要的是无论做什么，我们一定是要为了解决了某件事某个问题的，所以基于事件的框架是为了解决尽量让CPU平稳的运行，避免出现GC。
由于实力问题GPU平稳的运行还做不到。
