<template>
	<div
		class="relative w-full h-screen overflow-hidden bg-gradient-to-br from-[#0f1631] to-[#2a1b3d]"
	>
		<canvas
			id="particleCanvas"
			ref="particleCanvas"
			class="absolute inset-0 w-full h-full z-0"
		></canvas>

		<!-- 登录 loading 遮罩层 -->
		<div v-if="loginLoading" class="login-loading-mask">
			<div class="login-loading-spinner"></div>
		</div>

		<div class="relative z-10 flex items-center justify-center h-full px-4">
			<div
				class="absolute top-8 left-8 text-white text-xl font-bold drop-shadow-[0_0_10px_rgba(88,199,250,0.8)]"
			>
				{{ title }}
			</div>

			<div
				class="w-full max-w-md p-8 rounded-2xl backdrop-blur-md bg-white/10 border border-white/20 shadow-xl relative animate-float"
			>
				<!-- 紫色柔光背景 -->
				<div
					class="absolute inset-0 -z-10 rounded-2xl opacity-40 blur-[10px] bg-gradient-to-r from-purple-500 via-violet-600 to-fuchsia-600 animate-glow"
				></div>

				<h2
					class="text-white text-2xl font-semibold text-center mb-6 drop-shadow-[0_0_5px_rgba(180,120,255,0.6)]"
				>
					{{ t('sys.login.loginButton') }}
				</h2>

				<el-form :model="formData" ref="formRef" label-position="top">
					<el-form-item>
						<label class="text-white/90 text-sm mb-2 block font-medium">
							{{ t('sys.login.userName') }}
						</label>
						<el-input
							v-model="formData.userName"
							:placeholder="t('sys.login.userName')"
							class="!bg-white/10 !text-white !placeholder-white/50"
						/>
					</el-form-item>

					<el-form-item>
						<label class="text-white/90 text-sm mb-2 block font-medium">
							{{ t('sys.login.smsCode') }}
						</label>
						<AuthCodeInput
							v-model="verificationCode"
							:email="formData.userName"
							:disabled="codeInputDisabled"
							@input-end="handleInputEnd"
							@send-email-success="handleSendEmailSuccess"
						/>
					</el-form-item>
				</el-form>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { reactive, ref, onMounted, onUnmounted } from 'vue';
import { useUserStore } from '@/stores/modules/user';
import { useI18n } from '@/hooks/useI18n';
import { type FormInstance } from 'element-plus';
import { useGlobSetting } from '@/settings';
import AuthCodeInput from '@/components/form/authCode/index.vue';

const { t } = useI18n();
const userStore = useUserStore();
const { title } = useGlobSetting();

const formRef = ref<FormInstance>();
const particleCanvas = ref<HTMLCanvasElement | null>(null);

const formData = reactive({
	userName: '',
});

const verificationCode = reactive<string[]>([]);

let particleSystem: { animationId: number; resize: (w: number, h: number) => void } | null = null;
let mouseMoveListenerAdded = false;

onMounted(() => {
	initParticleBackground();
	window.addEventListener('resize', handleResize);
});

onUnmounted(() => {
	window.removeEventListener('resize', handleResize);
	if (particleSystem) {
		cancelAnimationFrame(particleSystem.animationId);
	}
});

function handleResize() {
	if (!particleCanvas.value) return;

	// 取消当前动画
	if (particleSystem) {
		cancelAnimationFrame(particleSystem.animationId);
		particleSystem = null;
		mouseMoveListenerAdded = false;
	}

	// 完全重新初始化粒子系统
	initParticleBackground();
}

function initParticleBackground() {
	const canvas = particleCanvas.value;
	if (!canvas) return;

	// 确保canvas尺寸与窗口尺寸完全匹配
	canvas.width = window.innerWidth;
	canvas.height = window.innerHeight;

	const ctx = canvas.getContext('2d');
	if (!ctx) return;

	// 创建粒子的函数，可以指定数量和区域
	const createParticles = (count = 100, area = { width: canvas.width, height: canvas.height }) =>
		Array.from({ length: count }, () => ({
			x: Math.random() * area.width,
			y: Math.random() * area.height,
			size: Math.random() * 2 + 1,
			speedX: (Math.random() - 0.5) * 0.5,
			speedY: (Math.random() - 0.5) * 0.5,
			color: `hsl(${Math.random() * 60 + 220}, 100%, 70%)`,
		}));

	// 初始化粒子 - 根据屏幕尺寸调整粒子数量
	const particleCount = Math.min(
		500,
		Math.max(50, Math.floor((canvas.width * canvas.height) / 10000))
	);
	console.log('Creating particles:', particleCount);
	let particles = createParticles(particleCount);

	// 鼠标位置变量
	let mouseX = 0,
		mouseY = 0;

	// 使用普通变量记录是否已添加事件监听器
	if (!mouseMoveListenerAdded) {
		window.addEventListener('mousemove', (e) => {
			mouseX = e.clientX;
			mouseY = e.clientY;
		});

		mouseMoveListenerAdded = true;
	}

	function animate() {
		if (!ctx || !canvas) return;
		ctx.clearRect(0, 0, canvas.width, canvas.height);

		const margin = 50; // 出界多少距离后销毁
		const newParticles: typeof particles = [];

		for (const p of particles) {
			// 绘制粒子
			ctx.fillStyle = p.color;
			ctx.beginPath();
			ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
			ctx.fill();

			// 鼠标吸附
			const dx = mouseX - p.x;
			const dy = mouseY - p.y;
			const distance = Math.sqrt(dx * dx + dy * dy);
			if (distance < 100) {
				const angle = Math.atan2(dy, dx);
				const force = 0.1;
				p.speedX += Math.cos(angle) * force;
				p.speedY += Math.sin(angle) * force;
			} else {
				// 非吸附的粒子加入轻微漂移
				p.speedX += (Math.random() - 0.5) * 0.05;
				p.speedY += (Math.random() - 0.5) * 0.05;
			}

			// 更新位置
			p.x += p.speedX;
			p.y += p.speedY;

			// 阻尼
			p.speedX *= 0.99;
			p.speedY *= 0.99;

			// 出界判断（超出 margin 则销毁）
			if (
				p.x >= -margin &&
				p.x <= canvas.width + margin &&
				p.y >= -margin &&
				p.y <= canvas.height + margin
			) {
				newParticles.push(p);
			}
		}

		// 保持粒子数量一致（补充新粒子）
		while (newParticles.length < particleCount) {
			newParticles.push({
				x: Math.random() * canvas.width,
				y: Math.random() * canvas.height,
				size: Math.random() * 2 + 1,
				speedX: (Math.random() - 0.5) * 0.5,
				speedY: (Math.random() - 0.5) * 0.5,
				color: `hsl(${Math.random() * 60 + 260}, 100%, 70%)`,
			});
		}

		particles = newParticles;

		// 绘制连线
		ctx.strokeStyle = 'rgba(160, 130, 255, 0.1)';
		ctx.lineWidth = 0.5;
		for (let i = 0; i < particles.length; i++) {
			for (let j = i + 1; j < particles.length; j++) {
				const dx = particles[i].x - particles[j].x;
				const dy = particles[i].y - particles[j].y;
				const dist = Math.sqrt(dx * dx + dy * dy);
				if (dist < 100) {
					ctx.beginPath();
					ctx.moveTo(particles[i].x, particles[i].y);
					ctx.lineTo(particles[j].x, particles[j].y);
					ctx.stroke();
				}
			}
		}

		particleSystem!.animationId = requestAnimationFrame(animate);
	}
	particleSystem = {
		animationId: requestAnimationFrame(animate),
		resize: () => {}, // 不再使用这个方法，而是完全重新初始化
	};
}

const codeInputDisabled = ref(true);
const handleSendEmailSuccess = () => {
	codeInputDisabled.value = false;
};

const loginLoading = ref(false);
const handleInputEnd = async () => {
	if (loginLoading.value) return;
	loginLoading.value = true;
	try {
		await userStore.login(
			{
				email: formData.userName,
				verificationCode: verificationCode.join(''),
			},
			'code'
		);
	} finally {
		loginLoading.value = false;
	}
};
</script>

<style scoped>
@keyframes float {
	0%,
	100% {
		transform: translateY(0);
	}
	50% {
		transform: translateY(-10px);
	}
}

@keyframes glow {
	0%,
	100% {
		opacity: 0.5;
		filter: blur(10px);
	}
	50% {
		opacity: 0.7;
		filter: blur(15px);
	}
}

@keyframes input-particle {
	0% {
		transform: scale(1);
		opacity: 1;
	}
	100% {
		transform: scale(0) translate(20px, -20px);
		opacity: 0;
	}
}

.login-loading-mask {
	position: fixed;
	left: 0;
	top: 0;
	right: 0;
	bottom: 0;
	background: rgba(30, 30, 50, 0.6);
	z-index: 9999;
	display: flex;
	flex-direction: column;
	align-items: center;
	justify-content: center;
}
.login-loading-spinner {
	width: 48px;
	height: 48px;
	border: 5px solid #fff;
	border-top: 5px solid #a07aff;
	border-radius: 50%;
	animation: spin 1s linear infinite;
	margin-bottom: 16px;
}
@keyframes spin {
	0% {
		transform: rotate(0deg);
	}
	100% {
		transform: rotate(360deg);
	}
}
.login-loading-text {
	color: #fff;
	font-size: 18px;
	letter-spacing: 2px;
}
</style>
